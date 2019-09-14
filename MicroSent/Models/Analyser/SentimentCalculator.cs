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
                tweet.rating = token.negationRating * token.wordRating;
                if (tweet.isDefinitelySarcastic)
                {
                    tweet.rating *= token.ironyRating;
                }
                else
                {
                    tweet.rating *= -1;
                }
            }
        }
    }
}
