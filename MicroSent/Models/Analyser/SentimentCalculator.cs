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
            foreach(Token token in tweet.allTokens)
            {
                foreach (SubToken subToken in token.subTokens)
                {
                    float subTokenRating = token.negationRating * subToken.wordRating;
                    if (!tweet.isDefinitelySarcastic)
                    {
                        subTokenRating *= token.ironyRating;
                    }
                    else
                    {
                        subTokenRating *= -1;
                    }

                    if (token.hasRepeatedLetters)
                    {
                        subTokenRating *= 1.4f; //TODO
                    }
                    if (token.isAllUppercase)
                    {
                        subTokenRating *= 1.4f; //TODO
                    }

                    if (subTokenRating != 0)
                    {
                        if (subTokenRating > 0)
                        {
                            tweet.positiveRating += subTokenRating;
                        }
                        else
                        {
                            tweet.negativeRating += subTokenRating;
                        }
                    }
                }
            }
        }
    }
}
