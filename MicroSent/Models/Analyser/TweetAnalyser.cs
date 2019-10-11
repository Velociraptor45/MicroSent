using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class TweetAnalyser
    {
        private const string IronyString = "irony";
        private const string Questionmark = "?";

        private const float NegationRating = -1f;

        public TweetAnalyser()
        {

        }

        public void analyseFirstEndHashtagPosition(List<Token> tokens, Tweet tweet)
        {
            for(int i = tokens.Count - 1; i > 0; i--)
            {
                Token currentToken = tokens[i];
                Token previousToken = tokens[i - 1];
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

        public void checkforIrony(Tweet tweet)
        {
            if (isIronyEndHashtag(tweet))
            {
                // TODO: set irony rating
            }
        }

        private bool isIronyEndHashtag(Tweet tweet)
        {
            if (tweet.firstEndHashtagIndex == -1)
                return false;

            foreach(Token token in tweet.rest)
            {
                if(token.text == IronyString)
                {
                    return true;
                }
            }

            return false;
        }

        private List<int> getSentenceIndexesOfNegationWord(List<Token> sentenceTokens)
        {
            List<int> tokenIndexes = new List<int>();
            Regex negationWord = new Regex(@"\b(not|\bno(\b|n-))|\bn'?t\b");
            foreach (Token token in sentenceTokens)
            {
                MatchCollection matches = negationWord.Matches(token.text);
                if(matches.Count > 0)
                {
                    tokenIndexes.Add(token.indexInSentence);
                }
            }
            return tokenIndexes;
        }

        public void applyKWordNegation(Tweet tweet, int negatedWordDistance,
            bool negateLeftSide = true, bool negateRightSide = true, bool ignoreSentenceBoundaries = false)
        {
            foreach (List<Token> sentenceTokens in tweet.sentences)
            {
                foreach (int tokenSentenceIndex in getSentenceIndexesOfNegationWord(sentenceTokens))
                {
                    Token token = sentenceTokens[tokenSentenceIndex];

                    int firstNegationIndex = negateLeftSide ? token.indexInTweet - negatedWordDistance : token.indexInTweet;
                    int lastNegationIndex = negateRightSide ? token.indexInTweet + negatedWordDistance : token.indexInTweet;

                    //tweet boundaries
                    firstNegationIndex = firstNegationIndex < 0 ? 0 : firstNegationIndex;
                    lastNegationIndex = lastNegationIndex > tweet.tokenCount - 1 ? tweet.tokenCount - 1 : lastNegationIndex;

                    //sentence boundaries
                    if (!ignoreSentenceBoundaries)
                    {
                        if (sentenceTokens.Count(t => t.indexInTweet == firstNegationIndex) == 0)
                        {
                            firstNegationIndex = sentenceTokens.First().indexInTweet;
                        }
                        while (sentenceTokens.Count(t => t.indexInTweet == lastNegationIndex) == 0)
                        {
                            lastNegationIndex = sentenceTokens.Last().indexInTweet;
                        }
                    }

                    for (int i = firstNegationIndex; i <= lastNegationIndex; i++)
                    {
                        if (i == tokenSentenceIndex)
                        {
                            continue;
                        }
                        tweet.getTokenByIndex(i).negationRating = NegationRating;
                    }
                }
            }
        }

        public void applyEndHashtagNegation(Tweet tweet)
        {
            if (tweet.firstEndHashtagIndex == -1)
                return;

            Regex negationWord = new Regex(@"\b(not|\bnon?\b)|\bnever\b|(ai|are|ca|could|did|does|do|had|has|have|is|must|need|ought|shall|should|was|were|wo|would)nt\b");
            foreach(Token token in tweet.rest.Where(t => t.indexInTweet >= tweet.firstEndHashtagIndex))
            {
                if (token.isHashtag)
                {
                    foreach (SubToken subToken in token.subTokens)
                    {
                        Match match = negationWord.Match(subToken.text);
                        if (match.Success)
                        {
                            token.negationRating = NegationRating;
                            break;
                        }
                    }
                }
            }
        }

        public void applyParseTreeDependentNegation(Tweet tweet, bool negationBeforeQuestionmark)
        {
            for(int sentenceIndex = 0; sentenceIndex < tweet.sentences.Count; sentenceIndex++)
            {
                List<Token> sentenceTokens = tweet.sentences[sentenceIndex];
                if (!negationBeforeQuestionmark && sentenceTokens.Last().text == Questionmark)
                {
                    continue;
                }

                List<int> negationWordIndexes = getSentenceIndexesOfNegationWord(sentenceTokens);

                foreach(int negationWordIndex in negationWordIndexes)
                {
                    List<int> tokenSentenceIndexesToNegate = getNegationRangeIndexes(tweet.parseTrees[sentenceIndex], negationWordIndex);
                    foreach(int tokenSentenceIndexToNegate in tokenSentenceIndexesToNegate)
                    {
                        Token token = sentenceTokens.Where(t => t.indexInSentence == tokenSentenceIndexToNegate).ToList().FirstOrDefault();
                        if (token.indexInTweet > -1)
                        {
                            token.negationRating *= NegationRating;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Couldn't find token in sentence {sentenceIndex} with index {tokenSentenceIndexToNegate}");
                            Console.ResetColor();
                        }
                    }
                }
            }
        }

        private List<int> getNegationRangeIndexes(Node root, int negationWordSentenceIndex)
        {
            List<int> indexRange = new List<int>();
            depthSearch(root, negationWordSentenceIndex, indexRange);

            return indexRange;
        }

        private Tuple<int, int> depthSearch(Node node, int negationWordSentenceIndex, List<int> allIndexes)
        {
            int smallestChildrenIndex = int.MaxValue;
            int highestChildrenIndex = int.MinValue;
            
            if (node.children.Count == 0)
            {
                int thisTokenIndex = node.correspondingToken.indexInSentence;
                return new Tuple<int, int>(thisTokenIndex, thisTokenIndex);
            }
            else
            {
                foreach (var child in node.children)
                {
                    var indexRange = depthSearch(child, negationWordSentenceIndex, allIndexes);
                    if(smallestChildrenIndex > indexRange.Item1)
                        smallestChildrenIndex = indexRange.Item1;
                    if(highestChildrenIndex < indexRange.Item2)
                        highestChildrenIndex = indexRange.Item2;
                }
            }

            if (allIndexes.Count == 0
                && smallestChildrenIndex <= negationWordSentenceIndex && highestChildrenIndex >= negationWordSentenceIndex)
            {
                int amount = highestChildrenIndex - smallestChildrenIndex + 1;
                allIndexes.AddRange(Enumerable.Range(smallestChildrenIndex, amount));
            }

            return new Tuple<int, int>(smallestChildrenIndex, highestChildrenIndex);
        }
    }
}
