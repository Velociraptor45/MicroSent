﻿using LinqToTwitter;
using MicroSent.Models;
using MicroSent.Models.Analyser;
using MicroSent.Models.TwitterConnection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
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

        public HomeController(IOptions<TwitterCrawlerConfig> config)
        {
            twitterCrawler = new TwitterCrawler(config);
            tokenizer = new Tokenizer();
            tokenAnalyser = new TokenAnalyser();
            tweetAnalyser = new TweetAnalyser();
            wordRater = new WordRater();
            posTagger = new PosTagger();
        }

        public async Task<IActionResult> Index()
        {
            //List<Status> quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("AlanZucconi");
            List<Status> ironyHashtags = await twitterCrawler.searchFor("#irony", 200);
            //List<Status> quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("davidkrammer");
            List<Tweet> allTweets = new List<Tweet>();

            foreach (Status status in ironyHashtags)
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
                posTagger.tagTweet(ref tweet);

                allTweets.Add(tweet);
            }

            return View();
        }
    }
}
