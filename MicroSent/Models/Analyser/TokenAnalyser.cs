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

            Regex smileyRegex = new Regex(@"((:-?|=)(\)|\(|\||\/|(D\b))|\bD:|:\s[\)\(])");
            Regex emoticonRegex = new Regex(@"\\U[a-f0-9]{4,8}");
            MatchCollection smileyMatches = smileyRegex.Matches(tokentext);
            MatchCollection emoticonMatches = emoticonRegex.Matches(tokentext);

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
            else if (linkMatches.Count > 0)
            {
                token.isLink = true;
            }
            else if (punktuationMatches.Count > 0)
            {
                token.isPunctuation = true;
            }
            else if (sentenceStructureMatches.Count > 0)
            {
                token.isStructureToken = true;
            }
            else if (smileyMatches.Count > 0)
            {
                token.isSmiley = true;
            }
            else if (emoticonMatches.Count > 0)
            {
                token.isEmoticon = true;
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
            for (int i = 1; i < token.text.Length; i++)
            {
                char currentLetter = token.text[i];
                char lastLetter = token.text[i - 1];

                if (currentLetter == lastLetter)
                {
                    token.hasRepeatedLetters = true;
                    token.text = token.text.Remove(i, 1);
                    i--;
                }
            }
        }

        public void checkForLaughingExpression(ref Token token)
        {
            Regex hahaRegex = new Regex(@"a?(ha){2,}");
            Regex hihiRegex = new Regex(@"i?(hi){2,}");
            MatchCollection hahaMatches = hahaRegex.Matches(token.text);
            MatchCollection hihiMatches = hihiRegex.Matches(token.text);

            if(hahaMatches.Count > 0 || hihiMatches.Count > 0)
            {
                token.isLaughingExpression = true;
            }
        }

        private void analyseHashtag(string hashtag)
        {

        }
    }
}
