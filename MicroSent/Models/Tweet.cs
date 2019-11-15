using MicroSent.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models
{
    [Serializable]
    public class Tweet
    {
        public string fullText;
        public string userScreenName;
        public ulong statusID;

        public List<string> urls;
        public string linkedDomain = null;

        public int tokenCount;
        public int firstEndHashtagIndex;

        public bool isIronic = false;

        public List<List<Token>> sentences;
        public List<Token> rest;
          
        public List<Node> parseTrees;
          
        public float positiveRating;
        public float negativeRating;

        // only needed for evaluation of algorithm
        public Polarity annotatedPolarity;

        public Tweet(string fullText, string userScreenName, ulong statusID)
        {
            this.fullText = fullText;
            this.userScreenName = userScreenName;
            this.statusID = statusID;

            urls = new List<string>();

            sentences = new List<List<Token>>();
            rest = new List<Token>();
            parseTrees = new List<Node>();

            firstEndHashtagIndex = -1;
            positiveRating = 0f;
            negativeRating = 0f;
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

        public string getFullSentence(int index)
        {
            string fullSentence = "";

            foreach(Token token in sentences[index])
            {
                if (token == sentences[index][0] || token.isPunctuation)
                    fullSentence += token.text;
                else
                    fullSentence += $" {token.text}";
            }
            return fullSentence;
        }
    }
}
