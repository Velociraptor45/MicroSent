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

            if (useTotalThreshold)
            {
                if (tweet.positiveRating < TotalThreshold)
                    tweet.positiveRating = 0;
                if (tweet.negativeRating > -TotalThreshold)
                    tweet.negativeRating = 0;
            }
        }

        private void calculateTokenRating(Tweet tweet, Token token,
            bool useSingleThreshold, bool intensifyLastSentence)
        {
            float tokenRating;
            if (token.subTokens.Count > 0)
            {
                float wordRatingSum = token.subTokens.Sum(st => st.wordRating);
                tokenRating = token.negationRating * token.wordRating;
            }
            else
            {
                tokenRating = token.negationRating * token.wordRating;
            }

            //if (!tweet.isDefinitelySarcastic)
            //{
            //    subTokenRating *= token.ironyRating;
            //}
            //else
            //{
            //    subTokenRating *= -1;
            //}

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
