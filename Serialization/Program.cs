using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Serialization
{
    class Program
    {
        static void Main(string[] args)
        {
            //serializeDict();
            //serializeSmileys();
            serializeSlang();
        }

        static void serializeSlang()
        {
            SlangConverter slangConverter = new SlangConverter();
            slangConverter.convert("data/slang.txt", "data/slang.xml");
        }

        static void serializeSmileys()
        {
            SmileyEmojiConverter converter = new SmileyEmojiConverter();
            var smileyList = converter.loadSmileyList("./data/EmoticonLookupTable.txt");
            converter.serializeSmileys(smileyList, "./data/smileys.xml");
        }

        static void serializeEmojis()
        {
            SmileyEmojiConverter converter = new SmileyEmojiConverter();
            var list = converter.loadEmojiList("./data/emojis.csv");
            converter.serializeEmojiList(list, "./data/emojis.xml");
        }

        static void serializeDict()
        {
            DictBuilder dictBuilder = new DictBuilder();
            string inputPath = @"data\trainingdata.csv"; //@"data\SentiWordNet_3.0.0.txt"; //@"data\testdata.csv";
            string outputPath = @"data\trainingdata.xml"; //@"data\polarityLexicon.xml"; //@"data\testdata.xml";
            string rootName = "TrainingData"; //"SentiWords"; //"TestData";

            Console.WriteLine("Start serializing");

            //dictBuilder.buildSentiDict(inputPath);
            //dictBuilder.buildTestDict(inputPath);
            dictBuilder.buildTrainingList(inputPath, out List<Tuple<string, float>> trainingList);

            //serializeDict(rootName, outputPath, dictBuilder);
            serializeList(rootName, outputPath, trainingList);
        }

        static void serializeList(string rootName, string outputPath, List<Tuple<string, float>> list)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Item[]), new XmlRootAttribute() { ElementName = rootName });
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                xmlSerializer.Serialize(writer, list.Select(e => new Item() { key = e.Item1, value = e.Item2 }).ToArray());
            }
        }

        static void serializeDict(string rootName, string outputPath, DictBuilder dictBuilder)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Item[]), new XmlRootAttribute() { ElementName = rootName });
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                xmlSerializer.Serialize(writer, dictBuilder.dictionary.Select(e => new Item() { key = e.Key, value = e.Value }).ToArray());
            }
        }
    }



    public class DictBuilder
    {
        public Dictionary<string, float> dictionary { get; private set; }

        private const string IgnoreLine = "#";

        public DictBuilder()
        {
            dictionary = new Dictionary<string, float>();
        }

        public void buildTrainingList(string filePath, out List<Tuple<string, float>> list)
        {
            list = new List<Tuple<string, float>>();
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                string line;
                string separator = "\",\"";
                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] parts = line.Split(separator);
                    int partsLastIndex = parts.Length - 1;
                    if (parts[0].StartsWith("\""))
                    {
                        parts[0] = parts[0].Remove(0, 1);
                    }
                    if (parts[partsLastIndex].EndsWith("\""))
                    {
                        parts[partsLastIndex] = parts[partsLastIndex].Remove(parts[partsLastIndex].Length - 1, 1);
                    }

                    int rating = int.Parse(parts[0]);
                    string tweet = parts[5];

                    if (partsLastIndex != 5)
                    {
                        // break here - shouldn't happen
                        int a = 0;
                    }

                    tweet = tweet.Replace("&amp;", "&");
                    tweet = tweet.Replace("&lt;", "<");
                    tweet = tweet.Replace("&gt;", ">");

                    list.Add(new Tuple<string, float>(tweet, rating));
                    Console.WriteLine($"Added {tweet} with rating {rating}");
                }
            }
        }

        public void buildTestDict(string filePath)
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                string line;
                string separator = "\",\"";
                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] parts = line.Split(separator);
                    int partsLastIndex = parts.Length - 1;
                    if (parts[0].StartsWith("\""))
                    {
                        parts[0] = parts[0].Remove(0, 1);
                    }
                    if (parts[partsLastIndex].EndsWith("\""))
                    {
                        parts[partsLastIndex] = parts[partsLastIndex].Remove(parts[partsLastIndex].Length - 1, 1);
                    }

                    int rating = int.Parse(parts[0]);
                    string tweet = parts[5];

                    if(partsLastIndex != 5)
                    {
                        // break here - shouldn't happen
                        int a = 0;
                    }

                    tweet = tweet.Replace("&amp;", "&");
                    tweet = tweet.Replace("&lt;", "<");
                    tweet = tweet.Replace("&gt;", ">");

                    dictionary.Add(tweet, rating);
                    Console.WriteLine($"Added {tweet} with rating {rating}");
                }
            }
        }

        public void buildSentiDict(string filePath)
        {
            //https://stackoverflow.com/questions/1014535/float-parse-doesnt-work-the-way-i-wanted
            CultureInfo cultureInfo = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";

            Dictionary<string, int> duplicatedTerms = new Dictionary<string, int>();

            using (StreamReader streamReader = new StreamReader(filePath))
            {
                string line;
                int counter = 0;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line.StartsWith(IgnoreLine))
                        continue;

                    counter++;
                    //0: POS tag
                    //1: ID
                    //2: Positiv rating
                    //3: Negative rating
                    //4: Terms
                    //5: Gloss
                    string[] sections = line.Split('\t');
                    float rating = 0;
                    try
                    {
                        rating = float.Parse(sections[2], NumberStyles.Any, cultureInfo) - float.Parse(sections[3], NumberStyles.Any, cultureInfo);
                    }
                    catch(Exception e) { continue; }

                    List<string> terms = getListFromTermString(sections[4]);

                    foreach(string term in terms)
                    {
                        string finalTerm = term + "!" + sections[0];
                        if (dictionary.ContainsKey(finalTerm))
                        {
                            dictionary[finalTerm] += rating;
                            if (duplicatedTerms.ContainsKey(finalTerm))
                                duplicatedTerms[finalTerm]++;
                            else
                                duplicatedTerms.Add(finalTerm, 1);
                            Console.WriteLine($"{finalTerm} already existent");
                        }
                        else
                        {
                            dictionary.Add(finalTerm, rating);
                            Console.WriteLine($"Added {finalTerm} with rating {rating}");
                        }
                    }
                }
            }

            foreach(string term in duplicatedTerms.Keys)
            {
                dictionary[term] /= duplicatedTerms[term] + 1;
            }
        }

        private List<string> getListFromTermString(string term)
        {
            List<string> list = new List<string>();

            string[] words = term.Split(" ");
            foreach(string word in words)
            {
                list.Add(word.Split("#")[0]);
            }

            return list;
        }
    }
}
