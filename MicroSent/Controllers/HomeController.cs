using LinqToTwitter;
using MicroSent.Models;
using MicroSent.Models.Analyser;
using MicroSent.Models.Constants;
using MicroSent.Models.TwitterConnection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

        public HomeController(IOptions<TwitterCrawlerConfig> config)
        {
            posTagger = new PosTagger();
            twitterCrawler = new TwitterCrawler(config);
            tokenizer = new Tokenizer();
            tokenAnalyser = new TokenAnalyser();
            tweetAnalyser = new TweetAnalyser();
            wordRater = new WordRater();
            sentimentCalculator = new SentimentCalculator();
        }

        public async Task<IActionResult> Index()
        {
            List<Tweet> allTweets = new List<Tweet>();
            //allTweets = await getTweetsAsync();

            Tweet tw = new Tweet("@Men is so under control. Is this not cool? He's new #new #cool #wontbeveryinteresting", "aa", 0);
            allTweets.Add(tw);

            for (int tweetIndex = 0; tweetIndex < allTweets.Count; tweetIndex++)
            {
                Tweet tweet = allTweets[tweetIndex];
                tokenizer.splitIntoTokens(ref tweet);

                //////////////////////////////////////////////////////////////
                /// TEST AREA
                if (tweet.fullText.StartsWith("Please @msexcel, don't be jealous."))
                {
                    int a = 0;
                }
                //////////////////////////////////////////////////////////////

                for (int i = 0; i < tweet.allTokens.Count; i++)
                {
                    Token token = tweet.allTokens[i];
                    //single Token analysis
                    tokenAnalyser.analyseTokenType(ref token);
                    tokenAnalyser.splitToken(ref token);
                    tokenAnalyser.checkForUppercase(ref token);
                    tokenAnalyser.replaceAbbreviations(ref token);
                    if (!token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
                    {
                        tokenAnalyser.removeRepeatedLetters(ref token);
                        for (int j = 0; j < token.subTokens.Count; j++)
                        {
                            SubToken subToken = token.subTokens[j];
                            subToken.wordRating = wordRater.getWordRating(token.subTokens[j]);
                            token.subTokens[j] = subToken;
                        }
                    }

                    tweet.allTokens[i] = token;
                }

                //single tweet analysis
                tweetAnalyser.analyseFirstEndHashtagPosition(ref tweet);
                //tweetAnalyser.applyKWordNegation(ref tweet, NegationConstants.FOUR_WORDS);
                posTagger.cutIntoSentences(ref tweet);
                posTagger.parseTweet(ref tweet);
                tweetAnalyser.applyParseTreeDependentNegation(ref tweet, true);
                tweetAnalyser.applyEndHashtagNegation(ref tweet);

                sentimentCalculator.calculateFinalSentiment(ref tweet);

                allTweets[tweetIndex] = tweet;
            }

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
            foreach(Tweet tweet in allTweets)
            {
                Console.WriteLine("_______________________________________________________________");
                Console.WriteLine($"https://twitter.com/{tweet.userScreenName}/status/{tweet.userID}");
                Console.WriteLine(tweet.fullText);
                Console.WriteLine($"Positive Rating: {tweet.positiveRating}");
                //var tokensPositiv = tweet.allTokens.Where(t => t.wordRating * t.negationRating > 0);
                foreach (Token token in tweet.allTokens)
                {
                    foreach (SubToken subToken in token.subTokens.Where(st => st.totalRating > 0))
                    {
                        Console.Write(token.textBeforeSplittingIntoSubTokens + $"({subToken.totalRating}), ");
                    }
                }
                Console.WriteLine("");
                Console.WriteLine($"Negative Rating: {tweet.negativeRating}");
                //var tokensNegative = tweet.allTokens.Where(t => t.wordRating * t.negationRating < 0);
                foreach (Token token in tweet.allTokens)
                {
                    foreach (SubToken subToken in token.subTokens.Where(st => st.totalRating < 0))
                    {
                        Console.Write(token.textBeforeSplittingIntoSubTokens + $"({subToken.totalRating}), ");
                    }
                }
                Console.WriteLine("");
            }
        }
    }
}
