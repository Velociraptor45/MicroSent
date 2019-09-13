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
using OpenNLP.Tools.PosTagger;

namespace MicroSent.Controllers
{
    public class HomeController : Controller
    {
        private TwitterCrawler twitterCrawler;

        private Tokenizer tokenizer;

        public HomeController(IOptions<TwitterCrawlerConfig> config)
        {
            twitterCrawler = new TwitterCrawler(config);
            tokenizer = new Tokenizer();
        }

        public async Task<IActionResult> Index()
        {
            List<Status> quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("davidkrammer");
            List<Tweet> allTweets = new List<Tweet>();

            foreach (Status status in quotedRetweetStatuses)
            {
                Tweet t = new Tweet(status.FullText);
                tokenizer.splitIntoTokens(ref t);
                allTweets.Add(t);

                Console.WriteLine("--------------------------------------");
                foreach (Token token in t.allTokens)
                {
                    Console.WriteLine(token.text);
                }
            }

            return View();
        }
    }
}
