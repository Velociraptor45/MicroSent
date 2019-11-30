using MicroSent.Models.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace MicroSent.Models.Serialization
{
    public class Deserializer
    {
        XmlSerializer xmlSerializer;
        private string filePath;
        IFormatter formatter;

        public Deserializer(string rootElement, string filePath, Type type)
        {
            xmlSerializer = new XmlSerializer(type, new XmlRootAttribute() { ElementName = rootElement });
            this.filePath = filePath;
        }

        public Deserializer()
        {
            formatter = new BinaryFormatter();
        }

        public void deserializeDictionary(out Dictionary<string, float> dictionary)
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                dictionary = ((Item[])xmlSerializer.Deserialize(streamReader)).ToDictionary(e => e.key, e => e.value);
            }
        }

        public void deserializeLexiconExtension(out Dictionary<string, float> extensionLexicon)
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                extensionLexicon = ((List<Word>)xmlSerializer.Deserialize(streamReader)).ToDictionary(
                    w => w.word, e => e.negativeOccurences > e.positiveOccurences ? -RatingConstants.LEXICON_EXTENSION_WORD : RatingConstants.LEXICON_EXTENSION_WORD);
            }
        }

        public void deserializeList(out List<Item> list)
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                list = ((Item[])xmlSerializer.Deserialize(streamReader)).ToList();
            }
        }



        public List<Tweet> deserializeTweets(string filePath)
        {
            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return (List<Tweet>)formatter.Deserialize(stream);
            }
        }

        public void deserializeEmojiList(out List<Emoji> emojiList)
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                emojiList = (List<Emoji>)xmlSerializer.Deserialize(streamReader);
            }
        }

        public void deserializeSmileyList(out List<Smiley> smileyList)
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                smileyList = (List<Smiley>)xmlSerializer.Deserialize(streamReader);
            }
        }
    }
}
