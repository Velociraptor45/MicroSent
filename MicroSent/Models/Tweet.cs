using System.Collections.Generic;

namespace MicroSent.Models
{
    public struct Tweet
    {
        public string fullText;
        public List<Token> allTokens;
        public List<Token> relevantForAnalysis;

        public bool isDefinitelySarcastic;
        public float rating;

        public Tweet(string fullText)
        {
            this.fullText = fullText;
            allTokens = new List<Token>();
            relevantForAnalysis = new List<Token>();

            isDefinitelySarcastic = false;
            rating = 0f;
        }
    }
}
