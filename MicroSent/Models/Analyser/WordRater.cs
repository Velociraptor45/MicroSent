using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using MicroSent.Models.Serialization;
using MicroSent.Models.Configuration;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace MicroSent.Models.Analyser
{
    public class WordRater
    {
        #region private members
        private const string SentiLexiconRootName = "SentiWords";
        private const string LexiconExtensionRootName = "LexiconExtension";

        private const string SentWordLabelAdjective = "a";
        private const string SentWordLabelNoun = "n";
        private const string SentWordLabelAdverb = "r";
        private const string SentWordLabelVerb = "v";

        private Regex positiveEmojiDetection = new Regex(RegexConstants.POSITIVE_EMOJI_PATTERN);
        private Regex negativeEmojiDetection = new Regex(RegexConstants.NEGATIVE_EMOJI_PATTERN);
        private Regex positiveSmileyDetection = new Regex(RegexConstants.POSITIVE_SMILEY_PATTERN);
        private Regex negativeSmileyDetection = new Regex(RegexConstants.NEGATIVE_SMILEY_PATTERN);

        private static Dictionary<string, float> polarityDictionary;

        private Deserializer lexiconDeserializer = new Deserializer(SentiLexiconRootName, DataPath.POLARITY_LEXICON, typeof(Item[]));
        private Deserializer lexiconExtensionDeserializer = new Deserializer(LexiconExtensionRootName, DataPath.LEXICON_EXTENSION, typeof(List<Word>));

        private const float ValueNotFound = float.MinValue;

        IAlgorithmConfiguration configuration;
        #endregion

        #region constructors
        public WordRater(IAlgorithmConfiguration configuration)
        {
            this.configuration = configuration;
            if(polarityDictionary == null)
            {
                lexiconDeserializer.deserializeDictionary(out polarityDictionary);
                if (configuration.useExtendedLexicon)
                {
                    lexiconExtensionDeserializer.deserializeLexiconExtension(out Dictionary<string, float> extensionLexicon);
                    polarityDictionary = polarityDictionary.Concat(extensionLexicon).ToDictionary(pair => pair.Key, pair => pair.Value);
                }
            }
        }
        #endregion

        #region public methods
        public float getEmojiRating(Token token)
        {
            if (positiveEmojiDetection.Match(token.text).Success)
            {
                return RatingConstants.POSITIVE_EMOJI;
            }
            else if (negativeEmojiDetection.Match(token.text).Success)
            {
                return RatingConstants.NEGATIVE_EMOJI;
            }
            return RatingConstants.WORD_NEUTRAL;
        }

        public float getSmileyRating(Token token)
        {
            if (positiveSmileyDetection.Match(token.text).Success)
            {
                return RatingConstants.POSITIVE_SMILEY;
            }
            else if (negativeSmileyDetection.Match(token.text).Success)
            {
                return RatingConstants.NEGATIVE_SMILEY;
            }
            return RatingConstants.WORD_NEUTRAL;
        }

        public void setWordRating(Token token)
        {
            if (token.subTokens.Count > 0)
            {
                foreach (SubToken subToken in token.subTokens)
                {
                    subToken.wordRating = getWordRating(subToken.text, subToken.stemmedText, subToken.lemmatizedText, subToken.posLabel);
                }
            }
            else
            {
                token.wordRating = getWordRating(token.text, token.stemmedText, token.lemmatizedText, token.posLabel);
            }

            
        }
        #endregion

        #region private methods
        private float getWordRating(string text, string stemmedText, string lemmatizedText, PosLabels posLabel)
        {
            string sentiWordLabel = convertToSentiWordPosLabel(posLabel);

            float normalRating = getFittingRating(text, sentiWordLabel);
            if (normalRating == RatingConstants.WORD_NEUTRAL)
            {
                if (configuration.useStemmedText)
                {
                    return getFittingRating(stemmedText, sentiWordLabel);
                }
                else if (configuration.useLemmatizedText)
                {
                    return getFittingRating(lemmatizedText, sentiWordLabel);
                }
            }
            return normalRating;
        }

        private float getFittingRating(string wordToRate, string sentiWordLabel)
        {
            if ((sentiWordLabel == null || configuration.useOnlyAverageRatingScore) && configuration.useAvarageRatingScore)
            {
                return getAverateWordRating(wordToRate);
            }
            else
            {
                return getPreciseWordRating(wordToRate, sentiWordLabel);
            }
        }

        private float getPreciseWordRating(string wordToRate, string sentiWordLabel,
            float defaultValue = RatingConstants.WORD_NEUTRAL)
        {
            string dictionaryKey = $"{wordToRate}!{sentiWordLabel}";
            if (polarityDictionary.ContainsKey(dictionaryKey))
            {
                float rating = polarityDictionary[dictionaryKey];
                return rating;
            }
            return defaultValue;
        }

        private float getAverateWordRating(string wordToRate)
        {
            int validKeyAmount = 0;
            float rating = 0;
            float singleRating;

            singleRating = getPreciseWordRating(wordToRate, SentWordLabelAdjective, ValueNotFound);
            if(singleRating != ValueNotFound)
            {
                rating += singleRating;
                validKeyAmount++;
            }

            singleRating = getPreciseWordRating(wordToRate, SentWordLabelNoun, ValueNotFound);
            if (singleRating != ValueNotFound)
            {
                rating += singleRating;
                validKeyAmount++;
            }

            singleRating = getPreciseWordRating(wordToRate, SentWordLabelAdverb, ValueNotFound);
            if (singleRating != ValueNotFound)
            {
                rating += singleRating;
                validKeyAmount++;
            }

            singleRating = getPreciseWordRating(wordToRate, SentWordLabelVerb, ValueNotFound);
            if (singleRating != ValueNotFound)
            {
                rating += singleRating;
                validKeyAmount++;
            }

            if (validKeyAmount > 0)
                rating /= validKeyAmount;

            return rating;
        }

        private string convertToSentiWordPosLabel(PosLabels label)
        {
            switch (label)
            {
                case PosLabels.JJ:
                case PosLabels.JJR:
                case PosLabels.JJS:
                    return SentWordLabelAdjective;
                case PosLabels.NN:
                case PosLabels.NNP:
                case PosLabels.NNPS:
                case PosLabels.NNS:
                    return SentWordLabelNoun;
                case PosLabels.RB:
                case PosLabels.RBR:
                case PosLabels.RBS:
                    return SentWordLabelAdverb;
                case PosLabels.VB:
                case PosLabels.VBD:
                case PosLabels.VBG:
                case PosLabels.VBN:
                case PosLabels.VBP:
                case PosLabels.VBZ:
                    return SentWordLabelVerb;
            }
            return null;
        }
        #endregion
    }
}
