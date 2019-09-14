using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MicroSent.Models.Analyser
{
    public class TokenAnalyser
    {
        private const string HASHTAG = "#";
        private const string MENTION = "@";

        public TokenAnalyser()
        {

        }

        public Token analyseTokenType(Token token)
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

            return token;
        }

        private void analyseHashtag(string hashtag)
        {

        }
    }
}
