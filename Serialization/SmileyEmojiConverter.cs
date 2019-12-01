using System;
using System.IO;
using Serialization.Models;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Globalization;

namespace Serialization
{
    class SmileyEmojiConverter
    {
        public List<Emoji> loadEmojiList(string listPath)
        {
            //src: https://stackoverflow.com/questions/1014535/float-parse-doesnt-work-the-way-i-wanted
            CultureInfo cultureInfo = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";

            List<Emoji> emojis = new List<Emoji>();

            using (StreamReader file = new StreamReader(listPath))
            {
                file.ReadLine();
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    string unicodeCharacter = Char.ConvertFromUtf32(Convert.ToInt32(parts[0], 16));
                    int occurences = int.Parse(parts[1]);
                    float negativeScore = float.Parse(parts[2], NumberStyles.Any, cultureInfo);
                    float neutralScore = float.Parse(parts[3], NumberStyles.Any, cultureInfo);
                    float positiveScore = float.Parse(parts[4], NumberStyles.Any, cultureInfo);
                    float sentimentScore = float.Parse(parts[5], NumberStyles.Any, cultureInfo);

                    emojis.Add(new Emoji(unicodeCharacter, occurences, negativeScore, neutralScore, positiveScore, sentimentScore));
                }
            }
            return emojis;
        }

        public void serializeEmojiList(List<Emoji> emojis, string destinationPath)
        {
            //binary serialization works - deserialization in different project doesn't...
            //-> xml serialization is needed

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<Emoji>), new XmlRootAttribute() { ElementName = "emojis" });
            using (StreamWriter writer = new StreamWriter(destinationPath))
            {
                xmlSerializer.Serialize(writer, emojis);
            }
        }

        public void serializeSmileys(List<Smiley> smileys, string destinationPath)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<Smiley>), new XmlRootAttribute() { ElementName = "smileys" });
            using (StreamWriter writer = new StreamWriter(destinationPath))
            {
                xmlSerializer.Serialize(writer, smileys);
            }
        }

        public List<Smiley> loadSmileyList(string path)
        {
            List<Smiley> list = new List<Smiley>();
            using (StreamReader file = new StreamReader(path))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    var parts = line.Split('\t');
                    list.Add(new Smiley(parts[0], getPolarityFromSmileyRating(float.Parse(parts[1]))));
                }
            }
            return list;
        }

        private Polarity getPolarityFromSmileyRating(float rating)
        {
            if (rating > 0)
                return Polarity.Positive;
            else if (rating < 0)
                return Polarity.Negative;
            else
                return Polarity.Neutral;
        }
    }
}
