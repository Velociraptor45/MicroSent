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
            twitterCrawler = new TwitterCrawler(config);
            tokenizer = new Tokenizer();
            tokenAnalyser = new TokenAnalyser();
            tweetAnalyser = new TweetAnalyser();
            wordRater = new WordRater();
            posTagger = new PosTagger();
            sentimentCalculator = new SentimentCalculator();
        }

        public async Task<IActionResult> Index()
        {
            List<Tweet> allTweets = new List<Tweet>();
            List<Status> quotedRetweetStatuses = new List<Status>();
            List <Status> linkStatuses = new List<Status>();
            quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("AlanZucconi");
            //linkStatuses = await twitterCrawler.getLinks("AlanZucconi");
            //List<Status> ironyHashtags = await twitterCrawler.searchFor("#irony", 200);
            //quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("davidkrammer");

            foreach (Status status in quotedRetweetStatuses)
            {
                Tweet tweet = new Tweet(status.FullText, status.ScreenName, status.StatusID);
                if (status.FullText.StartsWith("Please @msexcel, don't be jealous."))
                {
                    int a = 0;
                }
                tokenizer.splitIntoTokens(ref tweet);

                for (int i = 0; i < tweet.allTokens.Count; i++)
                {
                    Token token = tweet.allTokens[i];
                    //single Token analysis
                    tokenAnalyser.analyseTokenType(ref token);
                    tokenAnalyser.splitToken(ref token);
                    tokenAnalyser.checkForUppercase(ref token);
                    tokenAnalyser.replaceAbbreviations(ref token);
                    if (!token.isHashtag && !token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
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
                tweetAnalyser.applyParseTreeDependentNegation(ref tweet);

                sentimentCalculator.calculateFinalSentiment(ref tweet);
                allTweets.Add(tweet);
            }

            printOnConsole(allTweets);

            return View();
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
                    foreach (SubToken subToken in token.subTokens.Where(st => st.wordRating * token.negationRating > 0))
                    {
                        Console.Write(token.textBeforeSplittingIntoSubTokens + ", ");
                    }
                }
                Console.WriteLine("");
                Console.WriteLine($"Negative Rating: {tweet.negativeRating}");
                //var tokensNegative = tweet.allTokens.Where(t => t.wordRating * t.negationRating < 0);
                foreach (Token token in tweet.allTokens)
                {
                    foreach (SubToken subToken in token.subTokens.Where(st => st.wordRating * token.negationRating < 0))
                    {
                        Console.Write(token.textBeforeSplittingIntoSubTokens + ", ");
                    }
                }
                Console.WriteLine("");
            }
        }
    }
}
