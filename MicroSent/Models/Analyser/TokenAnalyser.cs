using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class TokenAnalyser
    {
        private const string HASHTAG = "#";
        private const string MENTION = "@";

        private Dictionary<string, string> abbreviations = new Dictionary<string, string>();

        public TokenAnalyser()
        {
            abbreviations.Add("r", "are");
            abbreviations.Add("u", "you");
            abbreviations.Add("y", "why"); // need papers for this
        }

        public void analyseTokenType(ref Token token)
        {
            string tokentext = token.text;
            Regex linkRegex = new Regex(@"(https:\/\/(www\.)?|www\.)([\d\w]+[\.\/])+[\d\w\?\=]+");
            Regex puntuationRegex = new Regex(@"([\?!]+|\.+|,|:)");
            Regex sentenceStructureRegex = new Regex(@"(\(|\)|-)");
            MatchCollection linkMatches = linkRegex.Matches(tokentext);
            MatchCollection punktuationMatches = puntuationRegex.Matches(tokentext);
            MatchCollection sentenceStructureMatches = sentenceStructureRegex.Matches(tokentext);

            if (tokentext.StartsWith(HASHTAG))
            {
                token.isHashtag = true;
                token.text = tokentext.Remove(0, 1);
                //analyseHashtag
            }
            else if (tokentext.StartsWith(MENTION))
            {
                token.isMention = true;
                token.text = tokentext.Remove(0, 1);
            }
            else if(linkMatches.Count > 0)
            {
                token.isLink = true;
            }
            else if(punktuationMatches.Count > 0)
            {
                token.isPunctuation = true;
            }
            else if(sentenceStructureMatches.Count > 0)
            {
                token.isStructureToken = true;
            }
        }

        public void checkForUppercase(ref Token token)
        {
            if (token.text == "I")
                return;

            foreach (char letter in token.text)
            {
                if (!char.IsUpper(letter))
                {
                    return;
                }
            }

            token.text = token.text.ToLower();
            token.isAllUppercase = true;
        }

        public void replaceAbbreviations(ref Token token)
        {
            if (abbreviations.TryGetValue(token.text, out string value))
            {
                token.text = value;
            }
        }

        public void removeRepeatedLetters(ref Token token)
        {
            for(int i = 1; i < token.text.Length; i++)
            {
                char currentLetter = token.text[i];
                char lastLetter = token.text[i - 1];

                if(currentLetter == lastLetter)
                {
                    token.text = token.text.Remove(i, 1);
                    i--;
                }
            }
        }

        private void analyseHashtag(string hashtag)
        {

        }
    }
}
