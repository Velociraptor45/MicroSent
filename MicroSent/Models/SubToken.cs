using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using System;

namespace MicroSent.Models
{
    [Serializable]
    public class SubToken
    {
        public string text;
        public string stemmedText;
        public string lemmatizedText;
        public int indexInToken;

        public PosLabels posLabel;

        public float wordRating = RatingConstants.WORD_NEUTRAL;

        public SubToken(string text, int indexInToken)
        {
            this.text = text;
            this.stemmedText = "";
            this.lemmatizedText = "";
            this.indexInToken = indexInToken;
        }
    }
}
