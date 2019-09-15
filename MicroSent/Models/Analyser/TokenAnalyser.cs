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

            if(checkForHashtag(ref token))
            {
                return;
            }
            else if(checkForMention(ref token))
            {
                return;
            }
            else if(checkForLink(ref token))
            {
                return;
            }
            else if(checkForPunctuation(ref token))
            {
                return;
            }
            else if(checkForSentenceStructure(ref token))
            {
                return;
            }
            else if(checkForSmiley(ref token))
            {
                return;
            }
            else if(checkForEmoticon(ref token))
            {
                return;
            }
            else if(checkForLaughingExpression(ref token))
            {
                return;
            }
        }

        private bool checkForHashtag(ref Token token)
        {
            if (token.text.StartsWith(HASHTAG))
            {
                token.text = token.text.Remove(0, 1);
                //analyseHashtag
                return token.isHashtag = true;
            }
            return false;
        }

        private bool checkForMention(ref Token token)
        {
            if (token.text.StartsWith(MENTION))
            {
                token.text = token.text.Remove(0, 1);
                return token.isMention = true;
            }
            return false;
        }

        private bool checkForLink(ref Token token)
        {
            Regex linkRegex = new Regex(@"(https:\/\/(www\.)?|www\.)([\d\w]+[\.\/])+[\d\w\?\=]+");
            MatchCollection linkMatches = linkRegex.Matches(token.text);

            if (linkMatches.Count > 0)
            {
                return token.isLink = true;
            }
            return false;
        }

        private bool checkForPunctuation(ref Token token)
        {
            Regex puntuationRegex = new Regex(@"([\?!]+|\.+|,|:)");
            MatchCollection punktuationMatches = puntuationRegex.Matches(token.text);

            if (punktuationMatches.Count > 0)
            {
                return token.isPunctuation = true;
            }
            return false;
        }

        private bool checkForSentenceStructure(ref Token token)
        {
            Regex sentenceStructureRegex = new Regex(@"(\(|\)|-)");
            MatchCollection sentenceStructureMatches = sentenceStructureRegex.Matches(token.text);

            if (sentenceStructureMatches.Count > 0)
            {
                return token.isStructureToken = true;
            }
            return false;
        }

        private bool checkForSmiley(ref Token token)
        {
            Regex smileyRegex = new Regex(@"((:-?|=)(\)|\(|\||\/|(D\b))|\bD:|:\s[\)\(])");
            MatchCollection smileyMatches = smileyRegex.Matches(token.text);

            if (smileyMatches.Count > 0)
            {
                return token.isSmiley = true;
            }
            return false;
        }

        private bool checkForEmoticon(ref Token token)
        {
            Regex emoticonRegex = new Regex(@"\\U[a-f0-9]{4,8}");
            MatchCollection emoticonMatches = emoticonRegex.Matches(token.text);

            if (emoticonMatches.Count > 0)
            {
                return token.isEmoticon = true;
            }
            return false;
        }

        private bool checkForLaughingExpression(ref Token token)
        {
            Regex hahaRegex = new Regex(@"a?(ha){2,}");
            Regex hihiRegex = new Regex(@"i?(hi){2,}");
            MatchCollection hahaMatches = hahaRegex.Matches(token.text);
            MatchCollection hihiMatches = hihiRegex.Matches(token.text);

            if (hahaMatches.Count > 0 || hihiMatches.Count > 0)
            {
                return token.isLaughingExpression = true;
            }
            return false;
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
            for (int i = 2; i < token.text.Length; i++)
            {
                char currentLetter = token.text[i];
                char lastLetter = token.text[i - 1];
                char secondLastLetter = token.text[i - 2];

                if (currentLetter == lastLetter && currentLetter == secondLastLetter)
                {
                    token.hasRepeatedLetters = true;
                    token.text = token.text.Remove(i, 1);
                    i--;
                }
            }
        }

        private void analyseHashtag(string hashtag)
        {
            //TODO
        }
    }
}
