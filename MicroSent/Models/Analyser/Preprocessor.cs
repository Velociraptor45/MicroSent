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
            checkString("hey, f u u idiot");
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
                        string[] parts = line.Split("-");
                        if (parts.Length > 2)
                        {
                            int a = 0;
                        }
                        slangs.Add(parts[0], parts[1]);
                    }
                }
            }
        }

        private void checkString(string s)
        {
            foreach(string key in slangs.Keys)
            {
                Regex regex = new Regex($@"\b{key}\b");
                MatchCollection matches = regex.Matches(s);
                if(matches.Count > 0)
                    s.Replace(key, slangs[key]);
            }
        }
    }
}
