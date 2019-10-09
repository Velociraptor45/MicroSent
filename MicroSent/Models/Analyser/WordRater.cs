using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using MicroSent.Models.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace MicroSent.Models.Analyser
{
    public class WordRater
    {
        private const string FilePath = @"data\wordPolarity\";
        private const string LexiconFileName = "polarityLexicon.xml";
        private const string SentiLexiconRootName = "SentiWords";

        private const string SentWordLabelAdjective = "a";
        private const string SentWordLabelNoun = "n";
        private const string SentWordLabelAdverb = "r";
        private const string SentWordLabelVerb = "v";

        private static Dictionary<string, float> polarityDictionary;

        private Deserializer deserializer = new Deserializer(SentiLexiconRootName, FilePath + LexiconFileName);

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
                deserializer.loadDictionary(out polarityDictionary);
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

        public float getWordRating(Token token, bool useOnlyAverageScore = true)
        {
            string sentiWordLabel = convertToSentiWordPosLabel(token.posLabel);
            if(sentiWordLabel == null || useOnlyAverageScore)
            {
                //return 0;
                int validKeyAmount = 0;
                float rating = 0;

                string adjectiveKey = $"{token.text}!{SentWordLabelAdjective}";
                if (polarityDictionary.ContainsKey(adjectiveKey))
                {
                    rating += polarityDictionary[adjectiveKey];
                    validKeyAmount++;
                }

                string nounKey = $"{token.text}!{SentWordLabelNoun}";
                if (polarityDictionary.ContainsKey(nounKey))
                {
                    rating += polarityDictionary[nounKey];
                    validKeyAmount++;
                }

                string adverbKey = $"{token.text}!{SentWordLabelAdverb}";
                if (polarityDictionary.ContainsKey(adverbKey))
                {
                    rating += polarityDictionary[adverbKey];
                    validKeyAmount++;
                }

                string verbKey = $"{token.text}!{SentWordLabelVerb}";
                if (polarityDictionary.ContainsKey(verbKey))
                {
                    rating += polarityDictionary[verbKey];
                    validKeyAmount++;
                }

                if(validKeyAmount > 0)
                    rating /= validKeyAmount;

                return rating;
            }
            else
            {
                string dictionaryKey = $"{token.text}!{sentiWordLabel}";
                if (polarityDictionary.ContainsKey(dictionaryKey))
                {
                    float rating = polarityDictionary[dictionaryKey];
                    return rating;
                }
                return RatingConstants.WORD_NEUTRAL;
            }
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
