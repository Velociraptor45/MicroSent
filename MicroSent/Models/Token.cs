using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using System;
using System.Collections.Generic;

namespace MicroSent.Models
{
    [Serializable]
    public class Token
    {
        public string text;
        public string originalText;
        public string stemmedText;
        public string lemmatizedText;

        public int indexInTweet = -1;
        public int indexInSentence = -1;

        public PosLabels posLabel;

        public List<SubToken> subTokens = new List<SubToken>();

        //token type
        public bool isMention = false;
        public bool isLink = false;
        public bool isHashtag = false;
        public bool isPunctuation = false;
        public bool isStructureToken = false; // ')' '(' '-' '...'
        public bool isSmiley = false;
        public bool isEmoji = false;
        public bool isLaughingExpression = false;

        public bool ignoreInRating = false;

        //ratings
        public float totalRating;

        public float smileyRating = RatingConstants.NEUTRAL;
        public float emojiRating = RatingConstants.NEUTRAL;
        public float ironyRating = RatingConstants.NEUTRAL;
        public float negationRating = RatingConstants.NEUTRAL;
        public float wordRating = RatingConstants.WORD_NEUTRAL;

        //token format
        public bool isAllUppercase = false;
        public bool hasRepeatedLetters = false;

        public Token(string text, int position)
        {
            this.text = text;
            originalText = text;
            this.indexInTweet = position;
        }
    }
}
