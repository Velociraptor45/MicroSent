using OpenNLP.Tools.Parser;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models
{
    public class Tweet
    {
        private const int ExitCode = -2;
        private const int FoundTokenCode = -3;

        public string fullText;
        public string userScreenName;
        public ulong statusID;
          
        public int firstEndHashtagIndex;
          
        public List<List<Token>> sentences;
        public List<Token> rest;
          
        public List<Node> parseTrees;
          
        public float positiveRating;
        public float negativeRating;

        // only needed for evaluation of algorithm
        public float testRating;

        public Tweet(string fullText, string userScreenName, ulong statusID)
        {
            this.fullText = fullText;
            this.userScreenName = userScreenName;
            this.statusID = statusID;
            sentences = new List<List<Token>>();
            rest = new List<Token>();
            parseTrees = new List<Node>();

            firstEndHashtagIndex = -1;
            positiveRating = 0f;
            negativeRating = 0f;

            testRating = 0f;
        }

        public List<int> getAllSiblingsIndexes(int tokenIndexInSentence, int sentenceIndex)
        {
            List<int> siblingIndexes = new List<int>();
            depthSearch(parseTrees[sentenceIndex], tokenIndexInSentence, -1, siblingIndexes);
            return siblingIndexes;
        }

        private int depthSearch(Parse tree, int tokenIndexInSentence, int lastFoundIndex, List<int> siblingIndexes)
        {
            int smallestChildrenIndex = tokenIndexInSentence;
            int highestChildrenIndex = tokenIndexInSentence;
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
