using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSent.Models.Analyser
{
    public class VisualizationTransformer
    {
        #region public methods
        public void translateTweetsToRating(List<Tweet> tweets, out List<Rating> linkRatings, out List<Rating> accountRating)
        {
            linkRatings = new List<Rating>();
            accountRating = new List<Rating>();

            var distinctLinks = tweets.Where(t => t.linkedDomain != null).Select(t => t.linkedDomain).Distinct();
            var distinctAccounts = tweets.Where(t => t.referencedAccount != null).Select(t => t.referencedAccount).Distinct();

            createAndAddRatingsToList(tweets, distinctLinks, linkRatings, true);
            createAndAddRatingsToList(tweets, distinctAccounts, accountRating, false);
        }
        #endregion

        #region private methods
        private void createAndAddRatingsToList(List<Tweet> tweets, IEnumerable<string> entities, List<Rating> ratingList, bool isLink)
        {
            foreach (var entity in entities)
            {
                IEnumerable<Tweet> tweetsWithEntity = getTweetsWithEntity(entity, tweets, isLink);
                EntityTweets entityTweets = separateRatedTweets(entity, tweetsWithEntity);

                if (entityTweets.positive.Count() > 0)
                {
                    ratingList.Add(generateRating(entityTweets.positive, entity));
                }
                if (entityTweets.negative.Count() > 0)
                {
                    ratingList.Add(generateRating(entityTweets.negative, entity));
                }
                if (entityTweets.neutral.Count() > 0)
                {
                    ratingList.Add(generateRating(entityTweets.neutral, entity));
                }
            }
        }

        private IEnumerable<Tweet> getTweetsWithEntity(string entity, List<Tweet> tweets, bool isLink)
        {
            if (isLink)
                return tweets.Where(t => t.linkedDomain == entity);
            else
                return tweets.Where(t => t.referencedAccount == entity);
        }

        private EntityTweets separateRatedTweets(string entity, IEnumerable<Tweet> tweetsWithEntity)
        {
            var positiveRatedTweets = tweetsWithEntity.Where(t => getHigherTweetPolarity(t) > 0);
            var negativeRatedTweets = tweetsWithEntity.Where(t => getHigherTweetPolarity(t) < 0);
            var neutralRatedTweets = tweetsWithEntity.Where(t => getHigherTweetPolarity(t) == 0);

            return new EntityTweets(entity, positiveRatedTweets, negativeRatedTweets, neutralRatedTweets);
        }

        private Rating generateRating(IEnumerable<Tweet> polarTweets, string entity)
        {
            float averageRating = polarTweets.Average(t => getHigherTweetPolarity(t));
            return new Rating(entity, averageRating, polarTweets.Count());
        }

        private float getHigherTweetPolarity(Tweet tweet)
        {
            return tweet.positiveRating > Math.Abs(tweet.negativeRating) ? tweet.positiveRating : tweet.negativeRating;
        }
        #endregion

        private struct EntityTweets
        {
            public string entity { get; }

            public IEnumerable<Tweet> positive { get; }
            public IEnumerable<Tweet> negative { get; }
            public IEnumerable<Tweet> neutral { get; }

            public EntityTweets(string entity, IEnumerable<Tweet> positive, IEnumerable<Tweet> negative, IEnumerable<Tweet> neutral)
            {
                this.entity = entity;

                this.positive = positive;
                this.negative = negative;
                this.neutral = neutral;
            }
        }
    }
}
