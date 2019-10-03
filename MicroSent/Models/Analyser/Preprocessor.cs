using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MicroSent.Models.Analyser
{
    public class Preprocessor
    {
        private const string FilePath = @"data/slang/";
        private const string SlangFileName = "slang.txt";

        private Dictionary<string, string> slangs = new Dictionary<string, string>();

        public Preprocessor()
        {
            loadSlang(SlangFileName);
        }

        private void loadSlang(string fileName)
        {
            using (StreamReader streamReader = new StreamReader(FilePath + fileName))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line != "")
                    {
                        string[] parts = line.Split("---");
                        slangs.Add(parts[0], parts[1]);
                    }
                }
            }
        }

        public string replaceAbbrevations(string tweetText)
        {
            foreach(string key in slangs.Keys)
            {
                string regexString = key.Replace(".", "\\.")
                    .Replace("*", "\\*")
                    .Replace("\\", "\\\\")
                    .Replace("|", "\\>")
                    .Replace(">", "\\>")
                    .Replace("<", "\\<");

                Regex regex = new Regex($@" {regexString} ");
                MatchCollection matches = regex.Matches(tweetText);
                if (matches.Count > 0)
                {
                    Console.WriteLine($"{tweetText}:: Replaced {key}");
                    tweetText = tweetText.Replace(key, slangs[key]);
                }
            }
            return tweetText;
        }
    }
}
