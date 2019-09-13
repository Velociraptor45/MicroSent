using MicroSent.Models.Constants;
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

        public List<Token> hashtagSubTokens;

        public int smileyRating;
        public int emoticonRating;
        public int ironyRating;
        public int negationRating;
        public int wordRating;

        public Token(string text, int position)
        {
            this.text = text;
            originalText = text;
            this.position = position;

            isMention = false;
            isLink = false;
            isHashtag = false;

            hashtagSubTokens = new List<Token>();

            smileyRating = RatingConstants.NEUTRAL;
            emoticonRating = RatingConstants.NEUTRAL;
            ironyRating = RatingConstants.NEUTRAL;
            negationRating = RatingConstants.NEUTRAL;
            wordRating = RatingConstants.NEUTRAL;
        }
    }
}
