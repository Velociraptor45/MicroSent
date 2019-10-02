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

        public int positionInToken;

        public float wordRating;

        public SubToken(string text, int positionInToken)
        {
            this.text = text;
            this.originalText = text;
            posLabel = PosLabels.Default;

            totalRating = 0f;

            this.positionInToken = positionInToken;

            wordRating = RatingConstants.WORD_NEUTRAL;
        }
    }
}
