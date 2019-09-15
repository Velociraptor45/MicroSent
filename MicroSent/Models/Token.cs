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

        public List<Token> hashtagSubTokens;
        public PosLabels posLabel;

        //token type
        public bool isMention;
        public bool isLink;
        public bool isHashtag;
        public bool isPunctuation;
        public bool isStructureToken; // ')' '(' '-'
        public bool isSmiley;
        public bool isEmoticon;
        public bool isLaughingExpression;

        //ratings
        public float smileyRating;
        public float emoticonRating;
        public float ironyRating;
        public float negationRating;
        public float wordRating;

        //token format
        public bool isAllUppercase;
        public bool hasRepeatedLetters;

        public Token(string text, int position)
        {
            this.text = text;
            originalText = text;
            this.position = position;

            hashtagSubTokens = new List<Token>();
            posLabel = PosLabels.Default;

            isMention = false;
            isLink = false;
            isHashtag = false;
            isPunctuation = false;
            isStructureToken = false;
            isSmiley = false;
            isEmoticon = false;
            isLaughingExpression = false;

            smileyRating = RatingConstants.NEUTRAL;
            emoticonRating = RatingConstants.NEUTRAL;
            ironyRating = RatingConstants.NEUTRAL;
            negationRating = RatingConstants.NEUTRAL;
            wordRating = RatingConstants.NEUTRAL;

            isAllUppercase = false;
            hasRepeatedLetters = false;
        }
    }
}
