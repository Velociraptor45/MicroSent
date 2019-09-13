using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MicroSent.Models;
using Microsoft.Extensions.Options;
using LinqToTwitter;

namespace MicroSent.Controllers
{
    public class HomeController : Controller
    {
        private TwitterCrawler twitterCrawler;

        public HomeController(IOptions<TwitterCrawlerConfig> config)
        {
            twitterCrawler = new TwitterCrawler(config);
        }

        public async Task<IActionResult> Index()
        {
            List<Status> quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("davidkrammer");

            return View();
        }
    }
}
