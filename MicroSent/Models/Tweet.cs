using OpenNLP.Tools.Parser;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models
{
    public struct Tweet
    {
        private const int ExitCode = -2;
        private const int FoundTokenCode = -3;

        public string fullText;
        public string userScreenName;
        public ulong userID;
        public List<Token> allTokens;
        public List<Token> relevantForAnalysis;
        public List<Parse> parseTrees;

        public int firstEndHashtagIndex;
        public int sentenceCount;

        public bool isDefinitelySarcastic;
        public float positiveRating;
        public float negativeRating;

        public Tweet(string fullText, string userScreenName, ulong userID)
        {
            this.fullText = fullText;
            this.userScreenName = userScreenName;
            this.userID = userID;
            allTokens = new List<Token>();
            relevantForAnalysis = new List<Token>();
            parseTrees = new List<Parse>();

            firstEndHashtagIndex = -1;
            sentenceCount = 0;

            isDefinitelySarcastic = false;
            positiveRating = 0f;
            negativeRating = 0f;
        }

        public List<int> getAllSiblingsIndexes(int tokenIndexInSentence, int sentenceIndex)
        {
            List<int> siblingIndexes = new List<int>();
            depthSearch(parseTrees[sentenceIndex], tokenIndexInSentence, -1, siblingIndexes);
            return siblingIndexes;
        }

        private int depthSearch(Parse tree, int tokenIndexInSentence, int lastFoundIndex, List<int> siblingIndexes)
        {
            int smallestChildrenIndex = int.MaxValue;
            int highestChildrenIndex = int.MinValue;
            bool foundToken = false;
            if (tree.ChildCount == 0)
            {
                return lastFoundIndex + 1;
            }

            foreach (Parse child in tree.GetChildren())
            {
                lastFoundIndex = lastFoundIndex == FoundTokenCode ? tokenIndexInSentence : lastFoundIndex; //filter out FoundTokenCode
                lastFoundIndex = depthSearch(child, tokenIndexInSentence, lastFoundIndex, siblingIndexes);
                if (lastFoundIndex == ExitCode)
                {
                    return ExitCode;
                }
                else if (lastFoundIndex == FoundTokenCode)
                {
                    if (tree.ChildCount > 1)
                        foundToken = true;
                    else
                        return FoundTokenCode;
                }
                else if (lastFoundIndex == tokenIndexInSentence)
                {
                    return FoundTokenCode;
                }

                if (lastFoundIndex >= 0)
                {
                    if (lastFoundIndex < smallestChildrenIndex)
                        smallestChildrenIndex = lastFoundIndex;
                    else
                        highestChildrenIndex = lastFoundIndex;
                }
            }

            if (foundToken)
            {
                fillListWithIndexes(siblingIndexes, smallestChildrenIndex, highestChildrenIndex);
                return ExitCode;
            }

            return lastFoundIndex;
        }

        private void fillListWithIndexes(List<int> list, int startIndex, int endIndex)
        {
            if (endIndex == int.MinValue)
            {
                list.Add(startIndex);
            }
            else
            {
                int amount = (endIndex - startIndex) + 1;
                list.AddRange(Enumerable.Range(startIndex, amount));
            }
        }
    }
}
