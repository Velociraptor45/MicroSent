using LinqToTwitter;
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

        public async Task<List<Status>> getQuotedRetweets(string username)
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
                                  where tweet.Type == StatusType.User && tweet.ScreenName == username && tweet.TweetMode == TweetMode.Extended && tweet.Count == maxTweets && tweet.SinceID == since
                                  select tweet).ToListAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            do
            {
                allResults.AddRange(timeline.Where(t => t.IsQuotedStatus));
                maxID = timeline.Min(status => status.StatusID) - 1;

                if (maxID < prevMaxID)
                {
                    prevMaxID = maxID;
                }
                else
                {
                    break;
                }

                timeline = await(from tweet in twitterContext.Status
                                 where tweet.Type == StatusType.User && tweet.ScreenName == username && tweet.TweetMode == TweetMode.Extended && tweet.Count == maxTweets && tweet.SinceID == since && tweet.MaxID == maxID
                                 select tweet).ToListAsync();
            } while (timeline.Any());

            allResults = removeNormalRetweets(allResults);
            return allResults;
        }

        public async Task<Status> getSingleQuotedTweet(string username)
        {
            int maxTweets = 1000;
            ulong since = 1;
            List<Status> timeline = new List<Status>();

            try
            {
                timeline = await (from tweet in twitterContext.Status
                                  where tweet.Type == StatusType.User && tweet.ScreenName == username && tweet.TweetMode == TweetMode.Extended && tweet.Count == maxTweets && tweet.SinceID == since && tweet.IsQuotedStatus
                                  select tweet).ToListAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            timeline = removeNormalRetweets(timeline);
            return timeline.First();
        }

        private List<Status> removeNormalRetweets(List<Status> statuses)
        {
            return statuses.Where(s => !s.FullText.StartsWith("RT")).ToList();
        }
    }
}
