using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
                        tweet.firstEndHashtagIndex = currentToken.indexInTweet;
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
            if (tweet.firstEndHashtagIndex == -1)
                return false;

            for(int i = tweet.firstEndHashtagIndex; i < tweet.allTokens.Count; i++)
            {
                if(tweet.allTokens[i].text.ToLower() == IronyString)
                {
                    return true;
                }
            }

            return false;
        }

        public void applyKWordNegation(ref Tweet tweet, int negatedWordDistance,
            bool negateLeftSide = true, bool negateRightSide = true, bool intelligentNegation = true)
        {
            Regex negationWord = new Regex(@"\b((can)?not|no(\b|n))|(ai|are|ca|could|did|does|do|had|has|have|is|must|need|ought|shall|should|was|were|wo|would)n'?t\b");

            for(int j = 0; j < tweet.allTokens.Count; j++)// each(Token token in tweet.allTokens)
            {
                Token token = tweet.allTokens[j];
                MatchCollection matches = negationWord.Matches(token.text);
                if(matches.Count > 0)
                {
                    int tokenIndex = token.indexInTweet;
                    int firstNegationIndex = negateLeftSide ? tokenIndex - negatedWordDistance : tokenIndex;
                    int lastNegationIndex = negateRightSide ? tokenIndex + negatedWordDistance : tokenIndex;
                    firstNegationIndex = firstNegationIndex < 0 ? 0 : firstNegationIndex;
                    lastNegationIndex = lastNegationIndex > tweet.allTokens.Count - 1 ? tweet.allTokens.Count - 1 : lastNegationIndex;
                    for(int i = firstNegationIndex; i <= lastNegationIndex; i++)
                    {
                        if(i == tokenIndex)
                        {
                            continue;
                        }
                        Token newToken = tweet.allTokens[i];
                        newToken.negationRating = -1f;
                        tweet.allTokens[i] = newToken;
                    }
                }
            }
        }
    }
}
