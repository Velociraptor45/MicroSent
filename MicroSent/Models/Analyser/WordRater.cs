using MicroSent.Models.Constants;
using System.Collections.Generic;
using System.IO;

namespace MicroSent.Models.Analyser
{
    public class WordRater
    {
        private const string FilePath = @"data\wordPolarity\";
        private const string PositiveWordsFileName = "positive-words.txt";
        private const string NegativeWordsFileName = "negative-words.txt";
        private const string IgnoreLine = ";";

        private static List<string> positiveWords = new List<string>();
        private static List<string> negativeWords = new List<string>();

        public WordRater()
        {
            if(positiveWords.Count == 0)
            {
                loadWords(PositiveWordsFileName, positiveWords);
            }
            if(negativeWords.Count == 0)
            {
                loadWords(NegativeWordsFileName, negativeWords);
            }
        }

        private void loadWords(string fileName, List<string> list)
        {
            using(StreamReader streamReader = new StreamReader(FilePath + fileName))
            {
                string line;
                while((line = streamReader.ReadLine()) != null)
                {
                    if(!line.StartsWith(IgnoreLine) && line != "")
                        list.Add(line);
                }
            }
        }

        public float getWordRating(Token token)
        {
            if (positiveWords.Contains(token.text))
            {
                return RatingConstants.POSITIVE;
            }
            else if (negativeWords.Contains(token.text))
            {
                return RatingConstants.NEGATIVE;
            }
            return RatingConstants.NEUTRAL;
        }
    }
}
