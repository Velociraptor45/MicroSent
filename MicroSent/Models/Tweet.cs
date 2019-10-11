using OpenNLP.Tools.Parser;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models
{
    public class Tweet
    {
        public string fullText;
        public string userScreenName;
        public ulong statusID;

        public int tokenCount;
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

        public Token getTokenByIndex(int indexInTweet)
        {
            var tokenList = sentences.SelectMany(s => s).Where(t => t.indexInTweet == indexInTweet).ToList();
            if(tokenList.Count == 0)
            {
                tokenList = rest.Where(t => t.indexInTweet == indexInTweet).ToList();
                if (tokenList.Count == 0)
                    return null;
            }
            return tokenList.First();
        }
    }
}
