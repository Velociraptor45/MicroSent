using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MicroSent.Models.TwitterConnection;
using Microsoft.Extensions.Options;
using LinqToTwitter;
using MicroSent.Models.Analyser;
using MicroSent.Models;
using OpenNLP.Tools.Tokenize;
using OpenNLP.Tools.SentenceDetect;

namespace MicroSent.Controllers
{
    public class HomeController : Controller
    {
        private TwitterCrawler twitterCrawler;

        private Tokenizer tokenizer;
        private TokenAnalyser tokenAnalyser;
        private WordRater wordRater;
        private PosTagger posTagger;

        public HomeController(IOptions<TwitterCrawlerConfig> config)
        {
            twitterCrawler = new TwitterCrawler(config);
            tokenizer = new Tokenizer();
            tokenAnalyser = new TokenAnalyser();
            wordRater = new WordRater();
            posTagger = new PosTagger();
        }

        public async Task<IActionResult> Index()
        {
            List<Status> quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("AlanZucconi");
            //List<Status> quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("davidkrammer");
            List<Tweet> allTweets = new List<Tweet>();

            foreach (Status status in quotedRetweetStatuses)
            {
                Tweet tweet = new Tweet(status.FullText);
                tokenizer.splitIntoTokens(ref tweet);
                allTweets.Add(tweet);

                for (int i = 0; i < tweet.allTokens.Count; i++)
                {
                    Token token = tweet.allTokens[i];
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
                posTagger.tagTweet(ref tweet);
            }

            return View();
        }
    }
}
