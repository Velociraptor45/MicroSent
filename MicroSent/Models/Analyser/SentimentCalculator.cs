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
                float tokenRating = token.negationRating * token.wordRating;
                if (!tweet.isDefinitelySarcastic)
                {
                    tokenRating *= token.ironyRating;
                }
                else
                {
                    tokenRating *= -1;
                }

                if(token.hasRepeatedLetters)
                {
                    tokenRating *= 1.4f; //TODO
                }
                if(token.isAllUppercase)
                {
                    tokenRating *= 1.4f; //TODO
                }

                if(tokenRating != 0)
                {
                    if(tokenRating > 0)
                    {
                        tweet.positiveRating += tokenRating;
                    }
                    else
                    {
                        tweet.negativeRating += tokenRating;
                    }
                }
            }
        }
    }
}
