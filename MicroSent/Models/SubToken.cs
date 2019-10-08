using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models
{
    public struct SubToken
    {
        public string text;
        public string originalText;
        public PosLabels posLabel;

        public float totalRating;

        public int indexInTweet;
        public int indexInSentence;
        public int indexInToken;

        public float wordRating;

        public SubToken(string text, int indexInTweet, int indexInToken)
        {
            this.text = text;
            this.originalText = text;
            posLabel = PosLabels.Default;

            totalRating = 0f;

            this.indexInTweet = indexInTweet;
            this.indexInSentence = -1;
            this.indexInToken = indexInToken;

            wordRating = RatingConstants.WORD_NEUTRAL;
        }
    }
}
