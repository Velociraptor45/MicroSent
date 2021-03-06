﻿using LinqToTwitter;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSent.Models.TwitterConnection
{
    public class TwitterCrawler
    {
        private IOptions<TwitterCrawlerConfig> config;
        private TwitterContext twitterContext;

        public TwitterCrawler(IOptions<TwitterCrawlerConfig> config)
        {
            this.config = config;
            Task t = authorize();
        }

        public async Task authorize()
        {
            var auth = new SingleUserAuthorizer()
            {
                CredentialStore = new SingleUserInMemoryCredentialStore
                {
                    ConsumerKey = config.Value.consumerKey,
                    ConsumerSecret = config.Value.consumerSecretKey,
                    AccessToken = config.Value.accessToken,
                    AccessTokenSecret = config.Value.accessTokenSecret
                }
            };

            await auth.AuthorizeAsync();

            twitterContext = new TwitterContext(auth);
        }

        public async Task<List<Status>> getLinksAndQuotedRetweets(string username)
        {
            int maxTweets = 10000;
            ulong since = 1;
            ulong maxID = 0;
            ulong prevMaxID = ulong.MaxValue;
            List<Status> allResults = new List<Status>();
            List<Status> timeline = new List<Status>();

            try
            {
                timeline = await (from tweet in twitterContext.Status
                                  where tweet.Type == StatusType.User && tweet.ScreenName == username
                                  && tweet.TweetMode == TweetMode.Extended
                                  && tweet.Count == maxTweets && tweet.SinceID == since && tweet.Lang == "en"
                                  && (tweet.Entities.UrlEntities.Count > 0 || tweet.IsQuotedStatus)
                                  select tweet).ToListAsync();
            }
            catch (TwitterQueryException e)
            {
                return null;
            }

            do
            {
                allResults.AddRange(timeline);
                maxID = timeline.Min(status => status.StatusID) - 1;

                if (maxID < prevMaxID)
                {
                    prevMaxID = maxID;
                }
                else
                {
                    break;
                }

                timeline = await (from tweet in twitterContext.Status
                                  where tweet.Type == StatusType.User && tweet.ScreenName == username
                                  && tweet.TweetMode == TweetMode.Extended
                                  && tweet.Count == maxTweets && tweet.SinceID == since
                                  && tweet.MaxID == maxID && tweet.Lang == "en"
                                  && (tweet.Entities.UrlEntities.Count > 0 || tweet.IsQuotedStatus)
                                  select tweet).ToListAsync();
            } while (timeline.Any());

            allResults = removeNormalRetweets(allResults);
            return allResults;
        }

        private List<Status> removeNormalRetweets(List<Status> statuses)
        {
            return statuses.Where(s => !s.FullText.StartsWith("RT")).ToList();
        }

        public async Task<List<Status>> searchFor(string query, int maxAmount)
        {
            int maxTweets = 1000;
            ulong prevMaxID = ulong.MaxValue;
            ulong maxID = 0;
            List<Status> allResults = new List<Status>();
            List<Status> results = new List<Status>();

            var directResults = await (from tweet in twitterContext.Search
                                       where tweet.Type == SearchType.Search && tweet.SearchLanguage == "en" 
                                       && tweet.Query == query && tweet.TweetMode == TweetMode.Extended && tweet.Count == maxTweets
                                       select tweet).ToListAsync();
            results = directResults.First().Statuses;

            do
            {
                allResults.AddRange(results);
                maxID = results.Min(status => status.StatusID) - 1;

                if (maxID < prevMaxID)
                {
                    prevMaxID = maxID;
                }
                else
                {
                    break;
                }

                directResults = await (from tweet in twitterContext.Search
                                  where tweet.Type == SearchType.Search && tweet.SearchLanguage == "en" && tweet.Query == query 
                                  && tweet.TweetMode == TweetMode.Extended && tweet.Count == maxTweets && tweet.MaxID == maxID
                                  select tweet).ToListAsync();
                results = directResults.First().Statuses;
                allResults = removeNormalRetweets(allResults);
            } while (results.Any() && allResults.Count < maxAmount);

            return allResults;
        }
    }
}
