using MicroSent.Models.Constants;
using MicroSent.Models.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class Preprocessor
    {
        private const string Separator = "---";

        private List<Slang> slangs;
        private Deserializer deserializer = new Deserializer("slang", DataPath.SLANG_FILE, typeof(List<Slang>));

        public Preprocessor()
        {
            loadSlang();
            generateSlangRegex();
        }

        private void loadSlang()
        {
            //using (StreamReader streamReader = new StreamReader(DataPath.SLANG_FILE))
            //{
            //    string line;
            //    while ((line = streamReader.ReadLine()) != null)
            //    {
            //        if (line != TokenPartConstants.EMPTY_STRING)
            //        {
            //            string[] parts = line.Split(Separator);
            //            slangs.Add(parts[0], parts[1]);
            //        }
            //    }
            //}

            deserializer.deserializeSlangList(out slangs);
        }

        private void generateSlangRegex()
        {
            string pattern = escapeRegexCharacters(slangs.First().slang);
            foreach (Slang slangObj in slangs.Skip(1))
            {
                pattern += $"|{escapeRegexCharacters(slangObj.slang)}";
            }
            RegexConstants.SLANG_PATTERN = pattern;
        }

        public void replaceAbbrevations(Tweet tweet)
        {
            Regex slangRegex = new Regex($@"(^| |""|'')({RegexConstants.SLANG_PATTERN})($| |""|'')", RegexOptions.Multiline);
            MatchCollection matches = slangRegex.Matches(tweet.fullText);
            foreach(Match match in matches)
            {
                Slang slangObj;
                try
                {
                    slangObj = slangs.Where(s => s.slang == match.Value.Trim()).First();
                } catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.StackTrace);
                    Console.ResetColor();
                    continue;
                }

                Console.WriteLine($"{tweet.fullText} :: Replaced {slangObj.slang} with {slangObj.replacement}");
                tweet.fullText = tweet.fullText.Replace(slangObj.slang, slangObj.replacement);
            }
        }

        private string escapeRegexCharacters(string text)
        {
            return text.Replace(".", "\\.")
                .Replace("*", "\\*")
                .Replace("\\", "\\\\")
                .Replace("|", "\\>")
                .Replace(">", "\\>")
                .Replace("<", "\\<");
        }
    }
}
