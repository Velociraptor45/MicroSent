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

        public async Task<IActionResult> Index()
        {
            List<Tweet> allTweets = new List<Tweet>();
            if (configuration.useSerializedData)
            {
                allTweets = deserializer.deserializeTweets(SerializedTweetsPath);
            }
            else if (configuration.testing)
            {
                allTweets = tester.getTestTweets().Skip(configuration.skipTweetsAmount).ToList();
            }
            else
            {
                allTweets = await getTweetsAsync("AlanZucconi");
                //allTweets = allTweets.Skip(56).ToList();
                //allTweets = await getTweetsAsync("davidkrammer");

                //Tweet tw = new Tweet("@Men is so under control. Is this not cool? He's new #new #cool #wontbeveryinteresting", "aa", 0);
                //Tweet tw = new Tweet("This is not a simple english sentence to understand the parser further.", "aa", 0);
                //Tweet tw = new Tweet("You are so GREAT! 🏃🏾‍♀️ :)", "aa", 0);
                //allTweets.Add(tw);
            }

            foreach (Tweet tweet in allTweets)
            {
                ConsolePrinter.printAnalysisStart(allTweets, tweet);
                if (!configuration.useSerializedData || !configuration.useGoogleParser)
                {
                    tweet.fullText = preprocessor.replaceAbbrevations(tweet.fullText);

                    //////////////////////////////////////////////////////////////
                    /// TEST AREA
                    //if (tweet.fullText.Contains("That didn't work out very well.")) //(tweet.fullText.StartsWith("Please @msexcel, don't be jealous."))
                    if (tweet.fullText.Contains("GO"))
                    {
                        int a = 0;
                    }
                    else if(allTweets.IndexOf(tweet) == 32)
                    {
                        int a = 0;
                    }
                    //////////////////////////////////////////////////////////////

                    List<Token> allTokens = tokenizer.splitIntoTokens(tweet);
                    tweet.tokenCount = allTokens.Count;

                    foreach (Token token in allTokens)
                    {
                        //single Token analysis

                        tokenAnalyser.analyseTokenType(token);
                        if (token.isHashtag)
                            tokenAnalyser.splitHashtag(token);
                        tokenAnalyser.checkForUppercase(token);
                        tokenAnalyser.convertToLowercase(token);
                        if (!token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
                        {
                            tokenAnalyser.removeRepeatedLetters(token);
                            tokenAnalyser.replaceMutatedVowel(token);
                            tokenAnalyser.stem(token);
                            tokenAnalyser.lemmatize(token);
                        }
                    }

                    //single tweet analysis
                    tweetAnalyser.analyseFirstEndHashtagPosition(allTokens, tweet);
                    posTagger.cutIntoSentences(tweet, allTokens);

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
                    }
                    else
                    {
                        foreach (var sentence in tweet.sentences)
                        {
                            Parse tree = posTagger.parseTweet(sentence);
                            Node rootNode = parseTreeAnalyser.translateToNodeTree(tree, tweet);
                            tweet.parseTrees.Add(rootNode);
                        }
                    }
                }


                if (!configuration.serializeData)
                {
                    if (tweet.urls.Count > 0)
                        tweet.linkedDomain = tweetAnalyser.extractDomain(tweet.urls.Last());

                    tweetAnalyser.filterUselessInterogativeSentences(tweet);

                    //////////////////////////////////////////////////////////////
                    /// NEGATION
                    //parseTreeAnalyser.applyGoogleParseTreeNegation(tweet);
                    //tweetAnalyser.applyParseTreeDependentNegation(tweet, true);
                    tweetAnalyser.applyKWordNegation(tweet, configuration.negationWindowSize);
                    tweetAnalyser.applySpecialStructureNegation(tweet);
                    tweetAnalyser.applyEndHashtagNegation(tweet);
                    //////////////////////////////////////////////////////////////

                    tweetAnalyser.checkforIrony(tweet);

                    applyRating(tweet);

                    sentimentCalculator.calculateFinalSentiment(tweet);
                }
            }

            if(configuration.serializeData && !configuration.useSerializedData)
                serializer.serializeTweets(allTweets, SerializedTweetsPath);


            if (configuration.testing)
                tester.checkTweetRating(allTweets);
            else
                printOnConsole(allTweets);

            if (!configuration.testing)
            {
                translateTweetsToRating(allTweets, out List<Rating> linkRatings, out List<Rating> accountRatings);
                return View(new HomeViewModel("", linkRatings, accountRatings));
            }
            return View();
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

        private async Task<List<Tweet>> getTweetsAsync(string accountName)
        {
            List<Tweet> allTweets = new List<Tweet>();
            List<Status> relevantStatuses = new List<Status>();

            ConsolePrinter.printBeginCrawlingTweets(accountName);
            relevantStatuses = await twitterCrawler.getLinksAndQuotedRetweets(accountName);
            ConsolePrinter.printFinishedCrawlingTweets();
            //relevantStatuses = await twitterCrawler.searchFor("#irony", 200);

            foreach(Status status in relevantStatuses)
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
                allTweets.Add(tweet);
            }

            return allTweets.Where(t => t.referencedAccount != null).ToList();
        }

        private void applyRating(Tweet tweet)
        {
            foreach (List<Token> sentence in tweet.sentences)
            {
                foreach (Token token in sentence)
                {
                    if (!token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
                    {
                        token.wordRating = wordRater.getWordRating(token);
                    }
                }
            }

            foreach(Token token in tweet.rest)
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

        private IEnumerable<string> getNoneTwitterUrls(Status status)
        {
            var noneTwitterUrls = status.Entities.UrlEntities.Where(e => !e.DisplayUrl.StartsWith(TokenPartConstants.TWITTER_DOMAIN));
            return noneTwitterUrls.Select(e => e.ExpandedUrl);
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
    }
}
