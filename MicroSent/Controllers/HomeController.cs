using LinqToTwitter;
using MicroSent.Models;
using MicroSent.Models.Analyser;
using MicroSent.Models.Constants;
using MicroSent.Models.Network;
using MicroSent.Models.Test;
using MicroSent.Models.TwitterConnection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        private NetworkClientSocket networkSendClientSocket;
        private NetworkClientSocket networkReceiveClientSocket;

        private Tester tester;

        private bool testing = true;
        private bool useGoogleParser = true;

        private int NetworkSendClientPort = 6048;
        private int NetworkReceiveClientPort = 6050;

        public HomeController(IOptions<TwitterCrawlerConfig> config)
        {
            posTagger = new PosTagger();
            twitterCrawler = new TwitterCrawler(config);
            tokenizer = new Tokenizer();
            tokenAnalyser = new TokenAnalyser();
            tweetAnalyser = new TweetAnalyser();
            wordRater = new WordRater();
            sentimentCalculator = new SentimentCalculator();
            preprocessor = new Preprocessor();

            networkSendClientSocket = new NetworkClientSocket(NetworkSendClientPort);
            networkReceiveClientSocket = new NetworkClientSocket(NetworkReceiveClientPort);

            tester = new Tester();
        }

        public async Task<IActionResult> Index()
        {
            List<Tweet> allTweets = new List<Tweet>();

            if(testing)
            {
                allTweets = tester.getTestTweets();
            }
            else
            {
                //allTweets = await getTweetsAsync();

                //Tweet tw = new Tweet("@Men is so under control. Is this not cool? He's new #new #cool #wontbeveryinteresting", "aa", 0);
                Tweet tw = new Tweet("This is not a simple english sentence to understand the parser further.", "aa", 0);
                allTweets.Add(tw);
            }

            foreach (Tweet tweet in allTweets.Skip(70))//.Where(t => t.fullText.Contains("and don't want you to die")))
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("###########################################################################################################");
                Console.WriteLine($"PROCESSING TWEET {allTweets.IndexOf(tweet) + 1}/{allTweets.Count}");
                Console.ResetColor();

                tweet.fullText = preprocessor.replaceAbbrevations(tweet.fullText);
                
                //////////////////////////////////////////////////////////////
                /// TEST AREA
                //if (tweet.fullText.Contains("That didn't work out very well.")) //(tweet.fullText.StartsWith("Please @msexcel, don't be jealous."))
                if (tweet.fullText.Contains("and don't want you to die"))
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
                    }
                }

                //single tweet analysis
                tweetAnalyser.analyseFirstEndHashtagPosition(allTokens, tweet);
                //tweetAnalyser.applyKWordNegation(tweet, NegationConstants.FOUR_WORDS);
                posTagger.cutIntoSentences(tweet, allTokens);

                List<JArray> parsedTokens = new List<JArray>();
                if (useGoogleParser)
                {
                    for (int i = 0; i < tweet.sentences.Count; i++)
                    {
                        networkSendClientSocket.sendStringToServer(tweet.getFullSentence(i)/*.Replace("'", "\'").Replace("\"", "\\\"")*/);
                        Task<string> serverAnswere = networkReceiveClientSocket.receiveParseTree();

                        await serverAnswere;
                        JObject treeJSON = JObject.Parse(serverAnswere.Result);

                        parsedTokens.Add(treeJSON.Value<JArray>(GoogleParserConstants.TOKEN_ARRAY));
                        posTagger.buildTreeFromGoogleParser(tweet, parsedTokens[i], i);
                    }
                }
                else
                {
                    posTagger.parseTweet(tweet);
                }

                //tweetAnalyser.applyParseTreeDependentNegation(tweet, true);
                tweetAnalyser.applyEndHashtagNegation(tweet);

                foreach (List<Token> sentence in tweet.sentences)
                {
                    foreach (Token token in sentence)
                    {
                        //single Token analysis
                        if (!token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
                        {
                            token.wordRating = wordRater.getWordRating(token);
                        }
                    }
                }

                sentimentCalculator.calculateFinalSentiment(tweet);
            }

            if (testing)
                tester.checkTweetRating(allTweets);
            else
                printOnConsole(allTweets);

            return View();
        }



        private async Task<List<Tweet>> getTweetsAsync()
        {
            List<Tweet> allTweets = new List<Tweet>();
            List<Status> quotedRetweetStatuses = new List<Status>();
            List<Status> linkStatuses = new List<Status>();
            quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("AlanZucconi");
            //linkStatuses = await twitterCrawler.getLinks("AlanZucconi");
            //List<Status> ironyHashtags = await twitterCrawler.searchFor("#irony", 200);
            //quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("davidkrammer");

            foreach(Status status in quotedRetweetStatuses)
            {
                Tweet tweet = new Tweet(status.FullText, status.ScreenName, status.StatusID);
                allTweets.Add(tweet);
            }

            return allTweets;
        }

        private void printOnConsole(List<Tweet> allTweets)
        {
            foreach (Tweet tweet in allTweets)
            {
                Console.WriteLine("_______________________________________________________________");
                Console.WriteLine($"https://twitter.com/{tweet.userScreenName}/status/{tweet.statusID}");
                Console.WriteLine(tweet.fullText);
                Console.WriteLine($"Positive Rating: {tweet.positiveRating}");
                //var tokensPositiv = tweet.allTokens.Where(t => t.wordRating * t.negationRating > 0);
                foreach (Token token in tweet.sentences.SelectMany(s => s).Where(t => t.totalRating > 0))
                {
                    Console.Write(token.text + $"({token.totalRating}), ");
                }
                Console.WriteLine("");
                Console.WriteLine($"Negative Rating: {tweet.negativeRating}");
                //var tokensNegative = tweet.allTokens.Where(t => t.wordRating * t.negationRating < 0);
                foreach (Token token in tweet.sentences.SelectMany(s => s).Where(t => t.totalRating < 0))
                {
                    Console.Write(token.text + $"({token.totalRating}), ");
                }
                Console.WriteLine("");
            }
        }
    }
}
