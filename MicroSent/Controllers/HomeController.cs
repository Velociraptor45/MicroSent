﻿using LinqToTwitter;
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
using OpenNLP.Tools.Parser;
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

        //private void test(Parse[] children)
        //{

        //    foreach (var child in children)
        //    {
        //        if (Enum.TryParse(child.Type.ToString(), out PosLabels label))
        //            continue;

        //        test(child.GetChildren());
        //    }
        //}

        public async Task<IActionResult> Index()
        {
            //EnglishTreebankParser nlpParser;
            //string nbinFilePath = @"data\NBIN_files\";
            //nlpParser = new EnglishTreebankParser(nbinFilePath, true, false);

            //Tweet tweet2 = new Tweet("I promise you will not regret it.");
            //tokenizer.splitIntoTokens(ref tweet2);

            //var parse = nlpParser.DoParse(tweet2.fullText);
            //tweet2.parseTrees.Add(parse.GetChildren()[0]);
            //var res = tweet2.getAllParentsChildIndexes(4, 0);
            //test(children);
            //return View();


            List<Status> quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("AlanZucconi");
            //List<Status> ironyHashtags = await twitterCrawler.searchFor("#irony", 200);
            //List<Status> quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("davidkrammer");
            List<Tweet> allTweets = new List<Tweet>();

            foreach (Status status in quotedRetweetStatuses)
            {
                Tweet tweet = new Tweet(status.FullText);
                tokenizer.splitIntoTokens(ref tweet);

                for (int i = 0; i < tweet.allTokens.Count; i++)
                {
                    Token token = tweet.allTokens[i];
                    //single Token analysis
                    tokenAnalyser.analyseTokenType(ref token);
                    tokenAnalyser.checkForUppercase(ref token);
                    tokenAnalyser.replaceAbbreviations(ref token);
                    if (!token.isHashtag && !token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
                    {
                        tokenAnalyser.removeRepeatedLetters(ref token);
                        token.wordRating = wordRater.getWordRating(token);
                    }

                    tweet.allTokens[i] = token;
                }
                //single tweet analysis
                tweetAnalyser.analyseFirstEndHashtagPosition(ref tweet);
                tweetAnalyser.applyKWordNegation(ref tweet, NegationConstants.FOUR_WORDS);
                posTagger.cutIntoSentences(ref tweet);
                posTagger.tagTweet(ref tweet);

                sentimentCalculator.calculateFinalSentiment(ref tweet);
                allTweets.Add(tweet);
                Console.WriteLine("_______________________________________________________________");
                Console.WriteLine($"https://twitter.com/{status.ScreenName}/status/{status.ID}");
                Console.WriteLine(tweet.fullText);
                Console.WriteLine($"Positive Rating: {tweet.positiveRating}");
                foreach (Token token in tweet.allTokens.Where(t => t.wordRating > 0))
                {
                    Console.Write(token.text + ", ");
                }
                Console.WriteLine("");
                Console.WriteLine($"Negative Rating: {tweet.negativeRating}");
                foreach (Token token in tweet.allTokens.Where(t => t.wordRating < 0))
                {
                    Console.Write(token.text + ", ");
                }
                Console.WriteLine("");
            }

            return View();
        }
    }
}
