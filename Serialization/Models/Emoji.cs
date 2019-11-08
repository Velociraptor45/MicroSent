using System;

namespace Serialization.Models
{
    [Serializable]
    class Emoji
    {
        public string unicodeCharacter;
        public int occurences;

        public float negativeScore;
        public float neutralScore;
        public float positiveScore;
        public float sentimentScore;

        public Emoji(string unicodeChar, int occurences, float negScore, float neutScore, float posScore, float sentiScore)
        {
            this.unicodeCharacter = unicodeChar;
            this.occurences = occurences;
            this.negativeScore = negScore;
            this.neutralScore = neutScore;
            this.positiveScore = posScore;
            this.sentimentScore = sentiScore;
        }
    }
}
