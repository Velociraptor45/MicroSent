using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MicroSent.Models.TwitterConnection;
using Microsoft.Extensions.Options;
using LinqToTwitter;
using MicroSent.Models.Analyser;
using MicroSent.Models;

namespace MicroSent.Controllers
{
    public class HomeController : Controller
    {
        private TwitterCrawler twitterCrawler;

        private Tokenizer tokenizer;
        private TokenAnalyser tokenAnalyser;
        private WordRater wordRater;

        public HomeController(IOptions<TwitterCrawlerConfig> config)
        {
            twitterCrawler = new TwitterCrawler(config);
            tokenizer = new Tokenizer();
            tokenAnalyser = new TokenAnalyser();
            wordRater = new WordRater();
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
                
                for(int i = 0; i < tweet.allTokens.Count; i++)
                {
                    Token token = tweet.allTokens[i];
                    token = tokenAnalyser.analyseToken(token);
                    if (!token.isHashtag && !token.isLink && !token.isMention)
                    {
                        token.wordRating = wordRater.getWordRating(tweet.allTokens[i]);
                    }

                    tweet.allTokens[i] = token;
                }
            }

            return View();
        }
    }
}
