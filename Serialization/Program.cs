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
            DictBuilder dictBuilder = new DictBuilder();

            Console.WriteLine("Start serializing");

            dictBuilder.buildSentiDict("data/sentiLexicon/lexicon.txt");

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Item[]), new XmlRootAttribute() { ElementName = "SentiWords" });
            using (StreamWriter writer = new StreamWriter("data/sentiLexicon/lexicon.xml"))
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


        public void buildSentiDict(string filePath)
        {
            //https://stackoverflow.com/questions/1014535/float-parse-doesnt-work-the-way-i-wanted
            CultureInfo cultureInfo = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";

            using (StreamReader streamReader = new StreamReader(filePath))
            {
                string line;
                int counter = 0;
                int skiped = 0;
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
                            skiped++;
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
