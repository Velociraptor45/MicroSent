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

        public void calculateFinalSentiment(ref Tweet tweet, bool useSingleThreshold = true, bool useTotalThreshold = true)
        {
            for(int i = 0; i < tweet.allTokens.Count; i++)
            {
                Token token = tweet.allTokens[i];
                for(int j = 0; j < token.subTokens.Count; j++)
                {
                    SubToken subToken = token.subTokens[j];
                    float subTokenRating = token.negationRating * subToken.wordRating;
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
                        subTokenRating *= 1.4f; //TODO
                    }
                    if (token.isAllUppercase)
                    {
                        subTokenRating *= 1.4f; //TODO
                    }

                    if(tweet.firstEndHashtagIndex != -1 && token.subTokens.First().indexInTweet >= tweet.firstEndHashtagIndex)
                    {
                        subTokenRating *= RatingConstants.END_HASHTAG_MULIPLIER;
                    }

                    if (subTokenRating != 0)
                    {
                        subToken.totalRating = subTokenRating;
                        if (subTokenRating > 0)
                        {
                            if (subTokenRating < SingleTokenThreshold && useSingleThreshold)
                            {
                                subToken.wordRating = RatingConstants.NEUTRAL;
                                subToken.totalRating = 0;
                            }
                            else
                            {
                                tweet.positiveRating += subTokenRating;
                            }
                        }
                        else
                        {
                            if (subTokenRating > -SingleTokenThreshold && useSingleThreshold)
                            {
                                subToken.wordRating = RatingConstants.NEUTRAL;
                                subToken.totalRating = 0;
                            }
                            else
                            {
                                tweet.negativeRating += subTokenRating;
                            }
                        }

                        token.subTokens[j] = subToken;
                    }
                }
                tweet.allTokens[i] = token;
            }

            if (useTotalThreshold)
            {
                if (tweet.positiveRating < TotalThreshold)
                    tweet.positiveRating = 0;
                if (tweet.negativeRating > -TotalThreshold)
                    tweet.negativeRating = 0;
            }
        }
    }
}
