using MicroSent.Models.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class Preprocessor
    {
        private const string Separator = "---";

        private Dictionary<string, string> slangs = new Dictionary<string, string>();

        public Preprocessor()
        {
            loadSlang();
        }

        private void loadSlang()
        {
            using (StreamReader streamReader = new StreamReader(DataPath.SLANG_FILE))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line != TokenPartConstants.EMPTY_STRING)
                    {
                        string[] parts = line.Split(Separator);
                        slangs.Add(parts[0], parts[1]);
                    }
                }
            }
        }

        public void replaceAbbrevations(Tweet tweet)
        {
            foreach(string key in slangs.Keys)
            {
                string regexString = key.Replace(".", "\\.")
                    .Replace("*", "\\*")
                    .Replace("\\", "\\\\")
                    .Replace("|", "\\>")
                    .Replace(">", "\\>")
                    .Replace("<", "\\<");

                Regex regex = new Regex($@"(^| |""|''){regexString}($| |""|'')", RegexOptions.Multiline);
                MatchCollection matches = regex.Matches(tweet.fullText);
                if (matches.Count > 0)
                {
                    Console.WriteLine($"{tweet.fullText} :: Replaced {key} with {slangs[key]}");
                    tweet.fullText = tweet.fullText.Replace(key, slangs[key]);
                }
            }
        }
    }
}
