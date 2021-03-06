﻿using MicroSent.Models.Constants;
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
        #region private members
        private List<Slang> slangs;
        private Deserializer deserializer = new Deserializer("slang", DataPath.SLANG_FILE, typeof(List<Slang>));
        #endregion

        #region constructors
        public Preprocessor()
        {
            loadSlang();
            generateSlangRegex();
        }
        #endregion

        #region public methods
        public void replaceAbbrevations(Tweet tweet)
        {
            Regex slangRegex = new Regex($@"(^| |""|'')({RegexConstants.SLANG_PATTERN})($| |""|'')", RegexOptions.Multiline);
            MatchCollection matches = slangRegex.Matches(tweet.fullText);
            foreach (Match match in matches)
            {
                Slang slangObj;
                try
                {
                    slangObj = slangs.Where(s => s.slang == match.Value.Trim()).First();
                }
                catch (Exception e)
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

        #endregion
        #region private methods
        private void loadSlang()
        {
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

        private string escapeRegexCharacters(string text)
        {
            return text.Replace(".", "\\.")
                .Replace("*", "\\*")
                .Replace("\\", "\\\\")
                .Replace("|", "\\>")
                .Replace(">", "\\>")
                .Replace("<", "\\<");
        }
        #endregion
    }
}
