using LinqToTwitter;
using MicroSent.Models.Configuration;
using MicroSent.Models.Constants;
using MicroSent.Models.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSent.Models.TwitterConnection
{
    public class TweetBuilder
    {
        #region private members
        private IAlgorithmConfiguration configuration;
        #endregion

        #region constructors
        public TweetBuilder(IAlgorithmConfiguration configuration)
        {
            this.configuration = configuration;
        }
        #endregion

        #region public methods
        public void setRandomReferencedAccounts(List<Tweet> tweets)
        {
            Random r = new Random();
            foreach (Tweet tweet in tweets)
            {
                tweet.referencedAccount = $"@testacc{r.Next(70)}";
            }
        }

        public List<Tweet> transformStatusesToTweets(List<Status> statuses)
        {
            List<Tweet> tweets = new List<Tweet>();
            foreach (Status status in statuses)
            {
                Tweet tweet = buildTweetFromStatus(status);
                if (tweet != null)
                    tweets.Add(tweet);
            }
            return tweets;
        }
        #endregion

        #region private methods
        private Tweet buildTweetFromStatus(Status status)
        {
            Tweet tweet = new Tweet(status.FullText, status.ScreenName, status.StatusID);
            tweet.urls.AddRange(getNoneTwitterUrls(status));
            if (status.IsQuotedStatus)
            {
                if (!setReferencedAccount(status, tweet))
                    return null; //quoted tweets without a referenced account are ... broken?
                                 //if you attempt to find those tweet, they don't exist -> skip them
            }
            return tweet;
        }

        private bool setReferencedAccount(Status status, Tweet tweet)
        {
            User referencedAccount = status.QuotedStatus.User;
            if (referencedAccount == null)
            {
                return false;
            }

            tweet.referencedAccount = $"@{referencedAccount.ScreenNameResponse}";
            return true;
        }

        private IEnumerable<string> getNoneTwitterUrls(Status status)
        {
            var noneTwitterUrls = status.Entities.UrlEntities.Where(e => !e.DisplayUrl.StartsWith(TokenPartConstants.TWITTER_DOMAIN));
            return noneTwitterUrls.Select(e => e.ExpandedUrl);
        }
        #endregion
    }
}
