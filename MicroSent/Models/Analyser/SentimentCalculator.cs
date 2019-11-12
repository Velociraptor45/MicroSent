using MicroSent.Models.Constants;
using MicroSent.Models.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models.Analyser
{
    public class SentimentCalculator
    {
        private IAlgorithmConfiguration configuration;

        public SentimentCalculator(IAlgorithmConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void calculateFinalSentiment(Tweet tweet)
        {
            foreach (List<Token> sentence in tweet.sentences)
            {
                foreach(Token token in sentence)
                {
                    calculateTokenRating(tweet, token);
                }
            }

            foreach (Token token in tweet.rest)
            {
                calculateTokenRating(tweet, token);
            }

            if (tweet.isIronic)
                invertRatings(tweet);

            applyTotalThreshold(tweet);
        }

        private void calculateTokenRating(Tweet tweet, Token token)
        {
            if (token.ignoreInRating)
                return;

            float tokenRating;
            if (token.subTokens.Count > 0)
            {
                float wordRatingSum = token.subTokens.Sum(st => st.wordRating);
                tokenRating = token.negationRating * token.wordRating;
            }
            else if (token.isEmoji)
            {
                tokenRating = token.emojiRating;
            }
            else if (token.isSmiley)
            {
                tokenRating = token.smileyRating;
            }
            else
            {
                tokenRating = token.negationRating * token.wordRating;
            }

            if (token.hasRepeatedLetters)
            {
                tokenRating *= RatingConstants.REPEATED_LETTER_MULTIPLIER;
            }
            if (token.isAllUppercase)
            {
                tokenRating *= RatingConstants.UPPERCASE_MULTIPLIER;
            }

            if (tweet.firstEndHashtagIndex != -1 && token.indexInTweet >= tweet.firstEndHashtagIndex)
            {
                tokenRating *= RatingConstants.END_HASHTAG_MULIPLIER;
            }


            //is token in last sentence?
            if (configuration.intensifyLastSentence && tweet.sentences.Last().Contains(token))
            {
                tokenRating *= RatingConstants.LAST_SENTENCE_INTENSIFIER;
            }

            setTokenRating(token, tweet, tokenRating);
        }

        private void invertRatings(Tweet tweet)
        {
            float negativeRating = tweet.negativeRating;
            tweet.negativeRating = -tweet.positiveRating;
            tweet.positiveRating = -negativeRating;
        }

        private void applyTotalThreshold(Tweet tweet)
        {
            if (configuration.useTotalThreshold)
            {
                if (tweet.positiveRating < configuration.totalThreshold)
                    tweet.positiveRating = 0;
                if (tweet.negativeRating > -configuration.totalThreshold)
                    tweet.negativeRating = 0;
            }
        }

        private void setTokenRating(Token token, Tweet tweet, float tokenRating)
        {
            token.totalRating = tokenRating;
            if (tokenRating > 0)
            {
                if (tokenRating < configuration.singleTokenThreshold && configuration.useSingleTokenThreshold)
                {
                    token.wordRating = RatingConstants.NEUTRAL;
                    token.totalRating = 0;
                }
                else
                {
                    tweet.positiveRating += tokenRating;
                }
            }
            else
            {
                if (tokenRating > -configuration.singleTokenThreshold && configuration.useSingleTokenThreshold)
                {
                    token.wordRating = RatingConstants.NEUTRAL;
                    token.totalRating = 0;
                }
                else
                {
                    tweet.negativeRating += tokenRating;
                }
            }
        }
    }
}
