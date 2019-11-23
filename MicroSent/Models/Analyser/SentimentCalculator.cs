﻿using MicroSent.Models.Constants;
using MicroSent.Models.Configuration;
using System.Collections.Generic;
using System.Linq;
using System;

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
                    if (!token.ignoreInRating)
                    {
                        float tokenRating = calculateTokenRating(tweet, token);
                        setTokenRating(token, tweet, tokenRating);
                    }
                }
            }

            foreach (Token token in tweet.rest)
            {
                if (!token.ignoreInRating)
                {
                    float tokenRating = calculateTokenRating(tweet, token);
                    setTokenRating(token, tweet, tokenRating);
                }
            }

            if (tweet.isIronic)
                invertRatings(tweet);


            if (configuration.useTotalThreshold)
            {
                applyTotalThreshold(tweet);
            }
        }

        private float calculateTokenRating(Tweet tweet, Token token)
        {
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

            return tokenRating;
        }

        private void invertRatings(Tweet tweet)
        {
            float negativeRating = tweet.negativeRating;
            tweet.negativeRating = -tweet.positiveRating;
            tweet.positiveRating = -negativeRating;
        }

        private void applyTotalThreshold(Tweet tweet)
        {
            if (tweet.positiveRating < configuration.totalThreshold)
                tweet.positiveRating = 0;
            if (tweet.negativeRating > -configuration.totalThreshold)
                tweet.negativeRating = 0;
        }

        private void setTokenRating(Token token, Tweet tweet, float tokenRating)
        {
            token.totalRating = tokenRating;
            if (Math.Abs(tokenRating) < configuration.singleTokenThreshold && configuration.useSingleTokenThreshold)
            {
                token.wordRating = RatingConstants.WORD_NEUTRAL;
                token.totalRating = 0;
            }
            else
            {
                if(tokenRating > 0)
                    tweet.positiveRating += tokenRating;
                else
                    tweet.negativeRating += tokenRating;
            }
        }
    }
}
