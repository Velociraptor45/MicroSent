using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
