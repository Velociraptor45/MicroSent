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
                if(tweet.allTokens[i].textBeforeSplittingIntoSubTokens.ToLower() == IronyString)
                {
                    return true;
                }
            }

            return false;
        }

        private List<int> getNegationWordIndexes(ref Tweet tweet, int sentenceIndex = -1)
        {
            List<int> tokenIndexes = new List<int>();
            Regex negationWord = new Regex(@"\b((can)?not|\bno(\b|n-))|(ai|are|ca|could|did|does|do|had|has|have|is|must|need|ought|shall|should|was|were|wo|would)n'?t\b");
            IEnumerable<Token> allTokens = sentenceIndex >= 0 ? tweet.allTokens.Where(t => t.sentenceIndex == sentenceIndex) : tweet.allTokens;
            foreach (Token token in allTokens)
            {
                int amountSubTokens = token.subTokens.Count;
                SubToken lastSubToken = token.subTokens.Last();
                SubToken secondLastSubToken;

                MatchCollection matches = negationWord.Matches(lastSubToken.text);
                if(matches.Count > 0)
                {
                    addIndexToList(token, tokenIndexes, sentenceIndex);
                }
                else if(amountSubTokens >= 2)
                {
                    secondLastSubToken = token.subTokens[amountSubTokens - 2];
                    matches = negationWord.Matches(secondLastSubToken.text + lastSubToken.text);
                    if(matches.Count > 0)
                    {
                        addIndexToList(token, tokenIndexes, sentenceIndex);
                    }
                }
            }

            return tokenIndexes;
        }

        private void addIndexToList(Token token, List<int> tokenIndexes, int sentenceIndex)
        {
            if (sentenceIndex >= 0)
            {
                tokenIndexes.Add(token.indexInSentence);
            }
            else
            {
                tokenIndexes.Add(token.indexInTweet);
            }
        }

        public void applyKWordNegation(ref Tweet tweet, int negatedWordDistance,
            bool negateLeftSide = true, bool negateRightSide = true, bool ignoreSentenceBoundaries = false)
        {
            foreach(int tokenIndex in getNegationWordIndexes(ref tweet))
            {
                Token token = tweet.allTokens[tokenIndex];
                int tokenSentenceIndex = token.sentenceIndex;

                int firstNegationIndex = negateLeftSide ? tokenIndex - negatedWordDistance : tokenIndex;
                int lastNegationIndex = negateRightSide ? tokenIndex + negatedWordDistance : tokenIndex;
                firstNegationIndex = firstNegationIndex < 0 ? 0 : firstNegationIndex;
                lastNegationIndex = lastNegationIndex > tweet.allTokens.Count - 1 ? tweet.allTokens.Count - 1 : lastNegationIndex;
                if (!ignoreSentenceBoundaries)
                {
                    while(tweet.allTokens[firstNegationIndex].sentenceIndex != token.sentenceIndex)
                    {
                        firstNegationIndex++;
                    }
                    while (tweet.allTokens[lastNegationIndex].sentenceIndex != token.sentenceIndex)
                    {
                        lastNegationIndex--;
                    }
                }

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

        public void applyEndHashtagNegation(ref Tweet tweet)
        {
            if (tweet.firstEndHashtagIndex == -1)
                return;

            Regex negationWord = new Regex(@"\b(not|\bnon?\b)|\bnever\b|(ai|are|ca|could|did|does|do|had|has|have|is|must|need|ought|shall|should|was|were|wo|would)nt\b");
            for (int i = tweet.firstEndHashtagIndex; i < tweet.allTokens.Count; i++)
            {
                Token token = tweet.allTokens[i];
                if (token.isHashtag)
                {
                    foreach (SubToken subToken in token.subTokens)
                    {
                        Match match = negationWord.Match(subToken.text);
                        if (match.Success)
                        {
                            token.negationRating = -1; //REPLACE
                            break;
                        }
                    }
                }
                tweet.allTokens[i] = token;
            }
        }

        public void applyParseTreeDependentNegation(ref Tweet tweet, bool negationBeforeQuestionmark)
        {
            for(int sentenceIndex = 0; sentenceIndex < tweet.sentenceCount; sentenceIndex++)
            {
                if(!negationBeforeQuestionmark && isLastSentenceTokenQuestionmark(tweet, sentenceIndex))
                {
                    continue;
                }

                List<Token> allTokensInSentence = tweet.allTokens.Where(t => t.sentenceIndex == sentenceIndex).ToList();
                List<int> negationWordSentenceIndexes = getNegationWordIndexes(ref tweet, sentenceIndex);

                foreach(int negationWordSentenceIndex in negationWordSentenceIndexes)
                {
                    var tokenIndexesInSentenceToNegate = tweet.getAllSiblingsIndexes(negationWordSentenceIndex, sentenceIndex);
                    foreach(int tokenIndexInSentenceToNegate in tokenIndexesInSentenceToNegate)
                    {
                        Token token = allTokensInSentence.Where(t => t.indexInSentence == tokenIndexInSentenceToNegate).First();
                        token.negationRating *= -1;
                        tweet.allTokens[token.indexInTweet] = token;
                    }
                }
            }
        }

        private bool isLastSentenceTokenQuestionmark(Tweet tweet, int sentenceIndex)
        {
            int lastSentenceTokenIndex = tweet.lastTokenIndexInSentence.GetValueOrDefault(sentenceIndex, -1);

            if(lastSentenceTokenIndex != -1)
            {
                //TODO: more questionmarks still valid?
                return tweet.allTokens[lastSentenceTokenIndex].subTokens.Last().text == "?";
            }
            return false;
        }
    }
}
