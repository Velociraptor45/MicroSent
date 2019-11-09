using MicroSent.Models.Constants;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models.Analyser
{
    public class SentimentCalculator
    {
        private const float SingleTokenThreshold = .25f;
        private const float TotalThreshold = .5f;

        public SentimentCalculator()
        {

        }

        public void calculateFinalSentiment(Tweet tweet,
            bool useSingleThreshold = true, bool useTotalThreshold = true, bool intensifyLastSentence = false)
        {
            foreach (List<Token> sentence in tweet.sentences)
            {
                foreach(Token token in sentence)
                {
                    calculateTokenRating(tweet, token, useSingleThreshold, intensifyLastSentence);
                }
            }

            foreach (Token token in tweet.rest)
            {
                calculateTokenRating(tweet, token, useSingleThreshold, intensifyLastSentence);
            }

            if (tweet.isIronic)
                invertRatings(tweet);

            applyTotalThreshold(tweet, useTotalThreshold);
        }

        private void calculateTokenRating(Tweet tweet, Token token,
            bool useSingleThreshold, bool intensifyLastSentence)
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
            if (intensifyLastSentence && tweet.sentences.Last().Contains(token))
            {
                tokenRating *= RatingConstants.LAST_SENTENCE_INTENSIFIER;
            }

            setTokenRating(token, tweet, tokenRating, useSingleThreshold);
        }

        private void invertRatings(Tweet tweet)
        {
            float negativeRating = tweet.negativeRating;
            tweet.negativeRating = -tweet.positiveRating;
            tweet.positiveRating = -negativeRating;
        }

        private void applyTotalThreshold(Tweet tweet, bool useTotalThreshold)
        {
            if (useTotalThreshold)
            {
                if (tweet.positiveRating < TotalThreshold)
                    tweet.positiveRating = 0;
                if (tweet.negativeRating > -TotalThreshold)
                    tweet.negativeRating = 0;
            }
        }

        private void setTokenRating(Token token, Tweet tweet, float tokenRating, bool useSingleThreshold)
        {
            token.totalRating = tokenRating;
            if (tokenRating > 0)
            {
                if (tokenRating < SingleTokenThreshold && useSingleThreshold)
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
                if (tokenRating > -SingleTokenThreshold && useSingleThreshold)
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
