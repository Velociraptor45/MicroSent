using MicroSent.Models.Constants;
using System;

namespace MicroSent.Models
{
    [Serializable]
    public class SubToken
    {
        public string text;
        public int indexInToken;

        public float wordRating = RatingConstants.WORD_NEUTRAL;

        public SubToken(string text, int indexInToken)
        {
            this.text = text;
            this.indexInToken = indexInToken;
        }
    }
}
