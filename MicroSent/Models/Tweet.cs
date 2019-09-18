using System.Collections.Generic;

namespace MicroSent.Models
{
    public struct Tweet
    {
        public string fullText;
        public List<Token> allTokens;
        public List<Token> relevantForAnalysis;

        public int firstEndHashtagIndex;

        public bool isDefinitelySarcastic;
        public float positiveRating;
        public float negativeRating;

        public Tweet(string fullText)
        {
            this.fullText = fullText;
            allTokens = new List<Token>();
            relevantForAnalysis = new List<Token>();

            firstEndHashtagIndex = -1;

            isDefinitelySarcastic = false;
            positiveRating = 0f;
            negativeRating = 0f;
        }
    }
}
