using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using System.Collections.Generic;

namespace MicroSent.Models
{
    public struct Token
    {
        public string text;
        public string originalText;
        public int position;

        public bool isMention;
        public bool isLink;
        public bool isHashtag;
        public bool isPunctuation;
        public bool isStructureToken; // ')' '(' '-'
        public bool isAllUppercase;

        public List<Token> hashtagSubTokens;

        public float smileyRating;
        public float emoticonRating;
        public float ironyRating;
        public float negationRating;
        public float wordRating;

        public PosLabels posLabel;

        public Token(string text, int position)
        {
            this.text = text;
            originalText = text;
            this.position = position;

            isMention = false;
            isLink = false;
            isHashtag = false;
            isPunctuation = false;
            isStructureToken = false;
            isAllUppercase = false;

            hashtagSubTokens = new List<Token>();

            smileyRating = RatingConstants.NEUTRAL;
            emoticonRating = RatingConstants.NEUTRAL;
            ironyRating = RatingConstants.NEUTRAL;
            negationRating = RatingConstants.NEUTRAL;
            wordRating = RatingConstants.NEUTRAL;

            posLabel = PosLabels.Default;
        }
    }
}
