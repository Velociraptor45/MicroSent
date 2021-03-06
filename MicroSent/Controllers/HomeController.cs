﻿using LinqToTwitter;
using MicroSent.Models;
using MicroSent.Models.Analyser;
using MicroSent.Models.Constants;
using MicroSent.Models.Test;
using MicroSent.Models.TwitterConnection;
using MicroSent.Models.Util;
using MicroSent.Models.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MicroSent.Models.Configuration;
using MicroSent.Models.RegexGeneration;
using MicroSent.ViewModels;
using MicroSent.Models.Enums;

namespace MicroSent.Controllers
{
    public class HomeController : Controller
    {
        #region private members
        private TwitterCrawler twitterCrawler;
        private TweetBuilder tweetBuilder;

        private Tokenizer tokenizer;
        private TokenAnalyser tokenAnalyser;
        private TweetAnalyser tweetAnalyser;
        private Rater rater;
        private PosTagger posTagger;
        private SentimentCalculator sentimentCalculator;
        private Preprocessor preprocessor;
        private ParseTreeBuilder parseTreeAnalyser;
        private Negator negator;
        private VisualizationTransformer visualizationTransformer;

        private Serializer serializer;
        private Deserializer deserializer;

        private RegexGenerator regexGenerator;

        private Tester tester;

        private const string SerializedTweetsPath = DataPath.SERIALIZED_TWEETS;

        private IAlgorithmConfiguration configuration;
        #endregion

        #region constructors
        public HomeController(IOptions<TwitterCrawlerConfig> crawlerConfig, IAlgorithmConfiguration algorithmConfiguration)
        {
            this.configuration = algorithmConfiguration;

            regexGenerator = new RegexGenerator(algorithmConfiguration);

            regexGenerator.generateEmojiRegexStrings();
            regexGenerator.generateSmileyRegexStrings();

            posTagger = new PosTagger(configuration);
            twitterCrawler = new TwitterCrawler(crawlerConfig);
            tweetBuilder = new TweetBuilder(configuration);
            tokenizer = new Tokenizer();
            tokenAnalyser = new TokenAnalyser();
            tweetAnalyser = new TweetAnalyser();
            rater = new Rater(algorithmConfiguration);
            sentimentCalculator = new SentimentCalculator(algorithmConfiguration);
            preprocessor = new Preprocessor();
            parseTreeAnalyser = new ParseTreeBuilder();
            negator = new Negator();
            visualizationTransformer = new VisualizationTransformer();

            serializer = new Serializer();
            deserializer = new Deserializer();

            tester = new Tester();
        }
        #endregion

        #region public methods
        public async Task<IActionResult> Index(HomeViewModel homeViewModel)
        {
            if (configuration.testing)
                homeViewModel.accountName = "test";

            if(homeViewModel.accountName == null || homeViewModel.accountName == "")
            {
                return View(homeViewModel);
            }

            List<Tweet> allTweets = await getTweets(homeViewModel.accountName);
            if (allTweets == null)
                return View(homeViewModel);

            await analyseTweets(allTweets);

            if(configuration.serializeData && !configuration.useSerializedData)
                serializer.serializeTweets(allTweets, SerializedTweetsPath);

            if (configuration.testing)
                tester.checkTweetRating(allTweets);
            else
                printOnConsole(allTweets);

            convertRatingsToVisualizationFormat(homeViewModel, allTweets);
            return View(homeViewModel);
        }
        #endregion

        #region private methods
        private void printOnConsole(List<Tweet> allTweets)
        {
            foreach (Tweet tweet in allTweets)
            {
                ConsolePrinter.printTweetAnalysisHead(tweet);
                ConsolePrinter.printPositiveRating(tweet);
                ConsolePrinter.printEmptyLine();
                ConsolePrinter.printNegativeRating(tweet);
                ConsolePrinter.printEmptyLine();
            }
        }

        private void convertRatingsToVisualizationFormat(HomeViewModel homeViewModel, List<Tweet> tweets)
        {
            visualizationTransformer.translateTweetsToRating(tweets, out List<Rating> linkRatings, out List<Rating> accountRatings);
            homeViewModel.linkRatings = linkRatings;
            homeViewModel.accountRatings = accountRatings;
        }

        #region ANALYSIS
        //////////////////////////////////////////////////////////////
        /// ANALYSIS
        //////////////////////////////////////////////////////////////

        private async Task analyseTweets(List<Tweet> tweets)
        {
            foreach (Tweet tweet in tweets)
            {
                ConsolePrinter.printAnalysisStart(tweets, tweet);
                if (!configuration.useSerializedData || !configuration.useGoogleParser)
                {
                    preprocessor.replaceAbbrevations(tweet);

                    List<Token> allTokens = tokenizer.splitIntoTokens(tweet);
                    tweet.tokenCount = allTokens.Count;

                    basicTokenAnalysis(allTokens);

                    //single tweet analysis
                    tweetAnalyser.analyseFirstEndHashtagPosition(allTokens, tweet);
                    posTagger.cutTokensIntoSentences(tweet, allTokens);

                    await posTagger.tagAllTokensOfTweet(tweet);

                    // converting to lowercase is important for matching words from the lexicon
                    // but lowercasing can only be applied after Pos-Tagging because
                    // the stanford parser has problems if all is lowercase
                    foreach (Token token in allTokens)
                        tokenAnalyser.convertToLowercase(token);
                }

                if (!configuration.serializeData)
                {
                    if (tweet.urls.Count > 0)
                        tweet.linkedDomain = tweetAnalyser.extractDomain(tweet.urls.Last());

                    tweetAnalyser.filterUselessInterogativeSentences(tweet);
                    applyNegation(tweet);
                    tweetAnalyser.checkforIrony(tweet);
                    applyRating(tweet);
                    sentimentCalculator.calculateFinalSentiment(tweet);
                }
            }
        }

        private void basicTokenAnalysis(List<Token> tokens)
        {
            foreach (Token token in tokens)
            {
                tokenAnalyser.analyseTokenType(token);
                if (token.isHashtag)
                    tokenAnalyser.splitHashtag(token);

                tokenAnalyser.checkForUppercase(token);
                if (!token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
                {
                    tokenAnalyser.removeRepeatedLetters(token);
                    tokenAnalyser.replaceMutatedVowel(token);
                    tokenAnalyser.stem(token);
                    tokenAnalyser.lemmatize(token);
                }
            }
        }

        private void applyNegation(Tweet tweet)
        {
            switch (configuration.negationType)
            {
                case NegationType.GoogleParseTree:
                    negator.applyGoogleParseTreeNegation(tweet);
                    break;
                case NegationType.TilNextPunctuation:
                    negator.applyNegationTilNextPunctuation(tweet);
                    break;
                case NegationType.KWindow:
                    negator.applyKWordNegation(tweet, configuration.negationWindowSize);
                    break;
            }
            negator.applySpecialStructureNegation(tweet);
            negator.applyEndHashtagNegation(tweet);
        }
        #endregion


        #region RATING
        //////////////////////////////////////////////////////////////
        /// RATING
        //////////////////////////////////////////////////////////////

        private void applyRating(Tweet tweet)
        {
            applyWordRating(tweet);
            applySmileyEmojiRating(tweet);
        }

        private void applyWordRating(Tweet tweet)
        {
            foreach (List<Token> sentence in tweet.sentences)
            {
                foreach (Token token in sentence)
                {
                    if (!token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
                    {
                        rater.setWordRating(token);
                    }
                }
            }
        }

        private void applySmileyEmojiRating(Tweet tweet)
        {
            foreach (Token token in tweet.rest)
            {
                if (token.isEmoji)
                {
                    token.emojiRating = rater.getEmojiRating(token);
                }
                else if (token.isSmiley)
                {
                    token.smileyRating = rater.getSmileyRating(token);
                }
            }
        }
        #endregion

        private async Task<List<Tweet>> getTweets(string accountName)
        {
            List<Tweet> tweets;
            if (configuration.useSerializedData)
            {
                tweets = deserializer.deserializeTweets(SerializedTweetsPath);
                tweetBuilder.setRandomReferencedAccounts(tweets);
            }
            else if (configuration.testing)
            {
                tweets = tester.getTestTweets().Skip(configuration.skipTweetsAmount).ToList();
                tweetBuilder.setRandomReferencedAccounts(tweets);
            }
            else
            {
                ConsolePrinter.printBeginCrawlingTweets(accountName);
                List<Status> statuses = await twitterCrawler.getLinksAndQuotedRetweets(accountName);
                ConsolePrinter.printFinishedCrawlingTweets();

                tweets = tweetBuilder.transformStatusesToTweets(statuses);
                if (tweets == null)
                    return null;
            }
            return tweets;
        }
        #endregion
    }
}
