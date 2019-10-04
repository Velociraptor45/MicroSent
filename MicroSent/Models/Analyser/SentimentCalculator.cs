using MicroSent.Models.Constants;
using System;
using System.Collections.Generic;

namespace MicroSent.Models.Analyser
{
    public class SentimentCalculator
    {

        public SentimentCalculator()
        {

        }

        public void calculateFinalSentiment(ref Tweet tweet)
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

                    if(tweet.firstEndHashtagIndex != -1 && token.indexInTweet >= tweet.firstEndHashtagIndex)
                    {
                        subTokenRating *= RatingConstants.END_HASHTAG_MULIPLIER;
                    }

                    if (subTokenRating != 0)
                    {
                        subToken.totalRating = subTokenRating;
                        if (subTokenRating > 0)
                        {
                            tweet.positiveRating += subTokenRating;
                        }
                        else
                        {
                            tweet.negativeRating += subTokenRating;
                        }

                        token.subTokens[j] = subToken;
                    }
                }
                tweet.allTokens[i] = token;
            }
        }
    }
}
