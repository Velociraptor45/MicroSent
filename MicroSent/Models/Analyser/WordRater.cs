using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using MicroSent.Models.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace MicroSent.Models.Analyser
{
    public class WordRater
    {
        private const string SentiLexiconRootName = "SentiWords";

        private const string SentWordLabelAdjective = "a";
        private const string SentWordLabelNoun = "n";
        private const string SentWordLabelAdverb = "r";
        private const string SentWordLabelVerb = "v";

        private Regex positiveEmojiDetection = new Regex(RegexConstants.POSITIVE_EMOJI_DETECTION);
        private Regex negativeEmojiDetection = new Regex(RegexConstants.NEGATIVE_EMOJI_DETECTION);
        private Regex positiveSmileyDetection = new Regex(RegexConstants.POSITIVE_SMILEY_DETECTION);
        private Regex negativeSmileyDetection = new Regex(RegexConstants.NEGATIVE_SMILEY_DETECTION);

        private static Dictionary<string, float> polarityDictionary;

        private Deserializer deserializer = new Deserializer(SentiLexiconRootName, DataPath.POLARITY_LEXICON, typeof(Item[]));

        private const float ValueNotFound = float.MinValue;

        //private const string PositiveWordsFileName = "positive-words.txt";
        //private const string NegativeWordsFileName = "negative-words.txt";
        //private const string IgnoreLine = ";";
        //private static List<string> positiveWords = new List<string>();
        //private static List<string> negativeWords = new List<string>();

        public WordRater()
        {
            
            //if (positiveWords.Count == 0)
            //{
            //    loadWords(PositiveWordsFileName, positiveWords);
            //}
            //if(negativeWords.Count == 0)
            //{
            //    loadWords(NegativeWordsFileName, negativeWords);
            //}
            if(polarityDictionary == null)
            {
                deserializer.deserializeDictionary(out polarityDictionary);
            }
        }

        //private void loadWords(string fileName, List<string> list)
        //{
        //    using(StreamReader streamReader = new StreamReader(FilePath + fileName))
        //    {
        //        string line;
        //        while((line = streamReader.ReadLine()) != null)
        //        {
        //            if(!line.StartsWith(IgnoreLine) && line != "")
        //                list.Add(line);
        //        }
        //    }
        //}

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
            return 0f; //TODO: change this
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
            return 0f; //TODO: change this
        }

        public float getWordRating(Token token, bool useOnlyAverageScore = false)
        {
            string sentiWordLabel = convertToSentiWordPosLabel(token.posLabel);

            float normalRating = getFittingRating(token.text, sentiWordLabel, useOnlyAverageScore);
            if (normalRating == RatingConstants.WORD_NEUTRAL)
            {
                return getFittingRating(token.stemmedText, sentiWordLabel, useOnlyAverageScore);
            }
            return normalRating;
        }

        private float getFittingRating(string wordToRate, string sentiWordLabel, bool useOnlyAverageScore)
        {
            if (sentiWordLabel == null || useOnlyAverageScore)
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
    }
}
