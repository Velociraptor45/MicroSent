using System;
using System.IO;
using Serialization.Models;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace Serialization
{
    class SmileyEmojiConverter
    {
        public List<Emoji> loadEmojiList(string listPath)
        {
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
                    float positiveScore = float.Parse(parts[2]);
                    float negativeScore = float.Parse(parts[3]);
                    float neutralScore = float.Parse(parts[4]);
                    float sentimentScore = float.Parse(parts[5]);

                    emojis.Add(new Emoji(unicodeCharacter, occurences, negativeScore, neutralScore, positiveScore, sentimentScore));
                }
            }
            return emojis;
        }

        public void serializeEmojiList(List<Emoji> emojis, string destinationPath)
        {
            using (Stream stream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, emojis);
            }
        }
    }
}
