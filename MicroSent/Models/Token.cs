using MicroSent.Models.Constants;

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

        public int smileyRating;
        public int emoticonRating;
        public int ironyRating;
        public int negationRating;

        public Token(string text, int position)
        {
            this.text = text;
            originalText = text;
            this.position = position;

            isMention = false;
            isLink = false;
            isHashtag = false;

            smileyRating = RatingConstants.NEUTRAL;
            emoticonRating = RatingConstants.NEUTRAL;
            ironyRating = RatingConstants.NEUTRAL;
            negationRating = RatingConstants.NEUTRAL;
        }
    }
}
