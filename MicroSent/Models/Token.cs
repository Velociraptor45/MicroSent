using MicroSent.Models.Constants;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models
{
    public struct Token
    {
        public string textBeforeSplittingIntoSubTokens;
        public string originalText;
        public int indexInTokenList;
        public int sentenceIndex;

        public List<SubToken> subTokens;

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

        //token format
        public bool isAllUppercase;
        public bool hasRepeatedLetters;

        public Token(string text, int position)
        {
            this.textBeforeSplittingIntoSubTokens = text;
            originalText = text;
            this.indexInTokenList = position;
            this.sentenceIndex = -1;

            subTokens = new List<SubToken>();

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

            isAllUppercase = false;
            hasRepeatedLetters = false;
        }
    }
}
