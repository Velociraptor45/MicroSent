﻿using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
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

        private const string SentWordLabelAdjective = "a";
        private const string SentWordLabelNoun = "n";
        private const string SentWordLabelAdverb = "r";
        private const string SentWordLabelVerb = "v";

        private static Dictionary<string, float> polarityDictionary = new Dictionary<string, float>();
        XmlSerializer xmlSerializer;


        //private const string PositiveWordsFileName = "positive-words.txt";
        //private const string NegativeWordsFileName = "negative-words.txt";
        //private const string IgnoreLine = ";";
        //private static List<string> positiveWords = new List<string>();
        //private static List<string> negativeWords = new List<string>();

        public WordRater()
        {
            xmlSerializer = new XmlSerializer(typeof(Item[]), new XmlRootAttribute() { ElementName = "SentiWords" });
            //if (positiveWords.Count == 0)
            //{
            //    loadWords(PositiveWordsFileName, positiveWords);
            //}
            //if(negativeWords.Count == 0)
            //{
            //    loadWords(NegativeWordsFileName, negativeWords);
            //}
            if(polarityDictionary.Count == 0)
            {
                loadDictionary(LexiconFileName);
            }
        }

        private void loadDictionary(string fileName)
        {
            using (StreamReader streamReader = new StreamReader(FilePath + fileName))
            {
                polarityDictionary = ((Item[])xmlSerializer.Deserialize(streamReader)).ToDictionary(e => e.key, e => e.value);
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

        public float getWordRating(SubToken subToken)
        {
            string sentiWordLabel = convertToSentiWordPosLabel(subToken.posLabel);
            if(sentiWordLabel == null)
            {
                int validKeyAmount = 0;
                float rating = 0;

                string adjectiveKey = $"{subToken.text}!{SentWordLabelAdjective}";
                if (polarityDictionary.ContainsKey(adjectiveKey))
                {
                    rating += polarityDictionary[adjectiveKey];
                    validKeyAmount++;
                }

                string nounKey = $"{subToken.text}!{SentWordLabelNoun}";
                if (polarityDictionary.ContainsKey(nounKey))
                {
                    rating += polarityDictionary[nounKey];
                    validKeyAmount++;
                }

                string adverbKey = $"{subToken.text}!{SentWordLabelAdverb}";
                if (polarityDictionary.ContainsKey(adverbKey))
                {
                    rating += polarityDictionary[adverbKey];
                    validKeyAmount++;
                }

                string verbKey = $"{subToken.text}!{SentWordLabelVerb}";
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
                string dictionaryKey = $"{subToken.text}!{sentiWordLabel}";
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
