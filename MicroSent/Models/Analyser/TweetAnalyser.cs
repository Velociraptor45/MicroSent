using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models.Analyser
{
    public class TweetAnalyser
    {
        private const string IronyString = "irony";

        public TweetAnalyser()
        {

        }

        public void analyseFirstEndHashtagPosition(ref Tweet tweet)
        {
            for(int i = tweet.allTokens.Count - 1; i > 0; i--)
            {
                Token currentToken = tweet.allTokens[i];
                Token previousToken = tweet.allTokens[i - 1];
                if (currentToken.isLink)
                {
                    continue;
                }
                else if(currentToken.isHashtag)
                {
                    if (!previousToken.isHashtag)
                    {
                        tweet.firstEndHashtagPosition = currentToken.position;
                    }
                    else
                    {
                        continue;
                    }
                }
                break;
            }
        }

        public void checkforIrony(ref Tweet tweet)
        {
            if (isIronyEndHashtag(tweet))
            {
                tweet.isDefinitelySarcastic = true;
            }
        }

        private bool isIronyEndHashtag(Tweet tweet)
        {
            if (tweet.firstEndHashtagPosition == -1)
                return false;

            for(int i = tweet.firstEndHashtagPosition; i < tweet.allTokens.Count; i++)
            {
                if(tweet.allTokens[i].text.ToLower() == IronyString)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
