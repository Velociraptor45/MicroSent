using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class Tokenizer
    {

        public Tokenizer()
        {

        }


        public List<Token> splitIntoTokens(Tweet tweet)
        {
            List<Token> allTokens = new List<Token>();
            int tokenIndex = 0;

            // link | smiley | emoticons | punctuation | wörter | sentence structure ()'-"
            Regex tokenDetection = new Regex(@"(https:\/\/(www\.)?|www\.)([\d\w]+[\.\/])+[\d\w\?\=]+|((:-?|=)(\)|\(|\||\/|(D\b))|\bD:|: [\)\(])|\\U[a-f0-9]{4,8}|([\?!]+|\.+|,|:)|(@|#[a-z]|\\)?(\w([''-]\w)?)+|(\(|\)|-|""|'')");
            Regex negationDetection = new Regex(@"\bcannot|(ai|are|ca|could|did|does|do|had|has|have|is|must|need|ought|shall|should|was|were|wo|would)nt\b");

            tweet.fullText = tweet.fullText.Replace('\n', ' ');

            MatchCollection tokenMatches = tokenDetection.Matches(tweet.fullText);
            MatchCollection negationMatches = negationDetection.Matches(tweet.fullText);

            foreach(Match match in tokenMatches)
            {
                string text = match.Value;
                if (negationMatches.Count > 0)
                {
                    string firstPart;
                    string secondPart;
                    if (text.EndsWith("nt"))
                    {
                        firstPart = text.Substring(0, text.Length - 2);
                        secondPart = text.Substring(text.Length - 2);
                    }
                    else
                    {
                        firstPart = text.Substring(0, text.Length - 3);
                        secondPart = text.Substring(text.Length - 3);
                    }

                    Token firstToken = new Token(firstPart, tokenIndex);
                    tokenIndex++;
                    Token secondToken = new Token(secondPart, tokenIndex);
                    tokenIndex++;
                    allTokens.Add(firstToken);
                    allTokens.Add(secondToken);
                    continue;
                }
                else if(tweet.fullText.Contains("'"))
                {
                    string[] parts = text.Split("'");
                    foreach(string part in parts)
                    {
                        Token token = new Token(part, tokenIndex);
                        allTokens.Add(token);
                        tokenIndex++;
                    }
                    continue;
                }
                Token t = new Token(text, tokenIndex);
                allTokens.Add(t);
            }

            return allTokens;
        }
    }
}
