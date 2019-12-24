using LinqToTwitter;
using MicroSent.Models;
using MicroSent.Models.Analyser;
using MicroSent.Models.Constants;
using MicroSent.Models.Network;
using MicroSent.Models.Test;
using MicroSent.Models.TwitterConnection;
using MicroSent.Models.Util;
using MicroSent.Models.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OpenNLP.Tools.Parser;
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
        private TwitterCrawler twitterCrawler;

        private Tokenizer tokenizer;
        private TokenAnalyser tokenAnalyser;
        private TweetAnalyser tweetAnalyser;
        private WordRater wordRater;
        private PosTagger posTagger;
        private SentimentCalculator sentimentCalculator;
        private Preprocessor preprocessor;
        private ParseTreeAnalyser parseTreeAnalyser;

        private NetworkClientSocket networkSendClientSocket;
        private NetworkClientSocket networkReceiveClientSocket;

        private Serializer serializer;
        private Deserializer deserializer;

        private RegexGenerator regexGenerator;

        private Tester tester;

        private const string SerializedTweetsPath = DataPath.SERIALIZED_TWEETS;

        private IAlgorithmConfiguration configuration;

        public HomeController(IOptions<TwitterCrawlerConfig> crawlerConfig, IAlgorithmConfiguration algorithmConfiguration)
        {
            this.configuration = algorithmConfiguration;

            regexGenerator = new RegexGenerator(algorithmConfiguration);

            regexGenerator.generateEmojiRegexStrings();
            regexGenerator.generateSmileyRegexStrings();

            posTagger = new PosTagger();
            twitterCrawler = new TwitterCrawler(crawlerConfig);
            tokenizer = new Tokenizer();
            tokenAnalyser = new TokenAnalyser();
            tweetAnalyser = new TweetAnalyser();
            wordRater = new WordRater(algorithmConfiguration);
            sentimentCalculator = new SentimentCalculator(algorithmConfiguration);
            preprocessor = new Preprocessor();
            parseTreeAnalyser = new ParseTreeAnalyser();

            networkSendClientSocket = new NetworkClientSocket(
                configuration.clientSendingPort, configuration.clientHost);
            networkReceiveClientSocket = new NetworkClientSocket(
                configuration.clientReceivingPort, configuration.clientHost);

            serializer = new Serializer();
            deserializer = new Deserializer();

            tester = new Tester();
        }

        public async Task<IActionResult> Index(HomeViewModel homeViewModel)
        {
            if (configuration.testing)
                homeViewModel.accountName = "test";

            if(homeViewModel.accountName == null || homeViewModel.accountName == "")
            {
                return View(homeViewModel);
            }

            List<Tweet> allTweets = await getTweets(homeViewModel);
            if (allTweets == null)
                return View(homeViewModel);

            await analyseTweets(allTweets);

            if(configuration.serializeData && !configuration.useSerializedData)
                serializer.serializeTweets(allTweets, SerializedTweetsPath);


            if (configuration.testing)
                tester.checkTweetRating(allTweets);
            else
                printOnConsole(allTweets);

            translateTweetsToRating(allTweets, out List<Rating> linkRatings, out List<Rating> accountRatings);
            homeViewModel.linkRatings = linkRatings;
            homeViewModel.accountRatings = accountRatings;
            return View(homeViewModel);
        }

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
                    posTagger.cutIntoSentences(tweet, allTokens);

                    await parseTweet(tweet);

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

        private async Task parseTweet(Tweet tweet)
        {
            if (configuration.useGoogleParser)
            {
                for (int i = 0; i < tweet.sentences.Count; i++)
                {
                    networkSendClientSocket.sendStringToServer(tweet.getFullUnicodeSentence(i));
                    Task<string> serverAnswere = networkReceiveClientSocket.receiveParseTree();

                    await serverAnswere;
                    JObject treeJSON = JObject.Parse(serverAnswere.Result);

                    JArray tokens = treeJSON.Value<JArray>(GoogleParserConstants.TOKEN_ARRAY);
                    parseTreeAnalyser.buildTreeFromGoogleParser(tweet, tokens, i);
                }
                for(int i = 0; i < tweet.rest.Count; i++)
                {
                    if (tweet.rest[i].isHashtag)
                    {
                        networkSendClientSocket.sendStringToServer(tweet.getFullUnicodeRestToken(i));
                        Task<string> serverAnswere = networkReceiveClientSocket.receiveParseTree();

                        await serverAnswere;
                        JObject treeJSON = JObject.Parse(serverAnswere.Result);
                        JArray parsedTokens = treeJSON.Value<JArray>(GoogleParserConstants.TOKEN_ARRAY);

                        for(int j = 0; j < parsedTokens.Count; j++)
                        {
                            JToken token = parsedTokens[j];
                            string tag = token.Value<string>(GoogleParserConstants.TOKEN_TAG);
                            try
                            {
                                tweet.rest[i].subTokens[j].posLabel = ParseTreeAnalyser.translateToPosLabel(tag);
                            }
                            catch(Exception e)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(e.StackTrace);
                                Console.ResetColor();
                            }
                        }
                    }
                }
            }
            else
            {
                posTagger.tagAllTokens(tweet);
            }
        }

        private void applyNegation(Tweet tweet)
        {
            switch (configuration.negationType)
            {
                case NegationType.GoogleParseTree:
                    parseTreeAnalyser.applyGoogleParseTreeNegation(tweet);
                    break;
                case NegationType.TilNextPunctuation:
                    tweetAnalyser.applyNegationTilNextPunctuation(tweet);
                    break;
                case NegationType.KWindow:
                    tweetAnalyser.applyKWordNegation(tweet, configuration.negationWindowSize);
                    break;
            }
            tweetAnalyser.applySpecialStructureNegation(tweet);
            tweetAnalyser.applyEndHashtagNegation(tweet);
        }
        #endregion


        #region RATING
        //////////////////////////////////////////////////////////////
        /// RATING
        //////////////////////////////////////////////////////////////

        private void applyRating(Tweet tweet)
        {
            foreach (List<Token> sentence in tweet.sentences)
            {
                foreach (Token token in sentence)
                {
                    if (!token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
                    {
                        wordRater.setWordRating(token);
                    }
                }
            }

            foreach (Token token in tweet.rest)
            {
                if (token.isEmoji)
                {
                    token.emojiRating = wordRater.getEmojiRating(token);
                }
                else if (token.isSmiley)
                {
                    token.smileyRating = wordRater.getSmileyRating(token);
                }
            }
        }

        private void translateTweetsToRating(List<Tweet> tweets, out List<Rating> linkRatings, out List<Rating> accountRating)
        {
            linkRatings = new List<Rating>();
            accountRating = new List<Rating>();

            var distinctLinks = tweets.Where(t => t.linkedDomain != null).Select(t => t.linkedDomain).Distinct();
            var distinctAccounts = tweets.Where(t => t.referencedAccount != null).Select(t => t.referencedAccount).Distinct();

            createAndAddRatingsToList(tweets, distinctLinks, linkRatings, true);
            createAndAddRatingsToList(tweets, distinctAccounts, accountRating, false);
        }

        private void createAndAddRatingsToList(List<Tweet> tweets, IEnumerable<string> entities, List<Rating> ratingList, bool isLink)
        {
            foreach (var entity in entities)
            {
                IEnumerable<Tweet> allTweetsContainingEntity;
                if (isLink)
                    allTweetsContainingEntity = tweets.Where(t => t.linkedDomain != null && t.linkedDomain == entity);
                else
                    allTweetsContainingEntity = tweets.Where(t => t.referencedAccount != null && t.referencedAccount == entity);

                var positiveRatedTweets = allTweetsContainingEntity.Where(t => getHigherTweetPolarity(t) > 0);
                var negativeRatedTweets = allTweetsContainingEntity.Where(t => getHigherTweetPolarity(t) < 0);
                var neutralRatedTweets = allTweetsContainingEntity.Where(t => getHigherTweetPolarity(t) == 0);

                int occurencesPositive = positiveRatedTweets.Count();
                int occurencesNegative = negativeRatedTweets.Count();
                int occurencesNeutral = neutralRatedTweets.Count();

                if (occurencesPositive > 0)
                {
                    float averageRatingPositive = positiveRatedTweets.Average(t => getHigherTweetPolarity(t));
                    Rating rating = new Rating(entity, averageRatingPositive, occurencesPositive);
                    ratingList.Add(rating);
                }
                if (occurencesNegative > 0)
                {
                    float averageRatingNegative = negativeRatedTweets.Average(t => getHigherTweetPolarity(t));
                    Rating rating = new Rating(entity, averageRatingNegative, occurencesNegative);
                    ratingList.Add(rating);
                }
                if (occurencesNeutral > 0)
                {
                    float averageRatingNeutral = neutralRatedTweets.Average(t => getHigherTweetPolarity(t));
                    Rating rating = new Rating(entity, averageRatingNeutral, occurencesNeutral);
                    ratingList.Add(rating);
                }
            }
        }

        private float getHigherTweetPolarity(Tweet tweet)
        {
            return tweet.positiveRating > Math.Abs(tweet.negativeRating) ? tweet.positiveRating : tweet.negativeRating;
        }
        #endregion


        #region TWEET CRAWLING
        //////////////////////////////////////////////////////////////
        /// TWEET CRAWLING
        //////////////////////////////////////////////////////////////

        private async Task<List<Tweet>> getTweets(HomeViewModel homeViewModel)
        {
            List<Tweet> tweets = new List<Tweet>();
            Random r = new Random();
            if (configuration.useSerializedData)
            {
                tweets = deserializer.deserializeTweets(SerializedTweetsPath);
                foreach (Tweet tweet in tweets)
                {
                    tweet.referencedAccount = $"@testacc{r.Next(70)}";
                }
            }
            else if (configuration.testing)
            {
                tweets = tester.getTestTweets().Skip(configuration.skipTweetsAmount).ToList();
                foreach (Tweet tweet in tweets)
                {
                    tweet.referencedAccount = $"@testacc{r.Next(70)}";
                }
            }
            else
            {
                tweets = await crawlTweetsAsync(homeViewModel.accountName);
                if (tweets == null)
                    return null;
            }
            return tweets;
        }

        private async Task<List<Tweet>> crawlTweetsAsync(string accountName)
        {
            ConsolePrinter.printBeginCrawlingTweets(accountName);
            List<Status> relevantStatuses = await twitterCrawler.getLinksAndQuotedRetweets(accountName);
            ConsolePrinter.printFinishedCrawlingTweets();

            if (relevantStatuses == null)
                return null;
            else
                return transformStatusesToTweets(relevantStatuses);
        }

        private List<Tweet> transformStatusesToTweets(List<Status> statuses)
        {
            List<Tweet> tweets = new List<Tweet>();
            foreach (Status status in statuses)
            {
                Tweet tweet = new Tweet(status.FullText, status.ScreenName, status.StatusID);
                tweet.urls.AddRange(getNoneTwitterUrls(status));
                if (status.IsQuotedStatus)
                {
                    User referencedAccount = status.QuotedStatus.User;
                    if (referencedAccount == null)
                        continue; //quoted tweets without a referenced account are ... broken?
                                  //if you attempt to find those tweet, they don't exist -> skip them
                    else
                        tweet.referencedAccount = $"@{referencedAccount.ScreenNameResponse}";
                }
                tweets.Add(tweet);
            }
            return tweets;
        }

        private IEnumerable<string> getNoneTwitterUrls(Status status)
        {
            var noneTwitterUrls = status.Entities.UrlEntities.Where(e => !e.DisplayUrl.StartsWith(TokenPartConstants.TWITTER_DOMAIN));
            return noneTwitterUrls.Select(e => e.ExpandedUrl);
        }
        #endregion
    }
}
