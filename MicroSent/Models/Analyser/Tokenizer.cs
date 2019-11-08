using MicroSent.Models.Constants;
using MicroSent.Models.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class Tokenizer
    {
        // link | smiley | emoticons | punctuation | wörter | sentence structure ()'-"
        Regex tokenDetection = new Regex(@"(https?:\/\/(www\.)?|www\.)([\d\w]+[\.\/])+[\d\w\?\=]+|((:-?|=)(\)|\(|\||\/|(D\b))|\bD:|: [\)\(])|\\U[a-f0-9]{4,8}|([\?!]+|\.+|,|:)|(@|#[a-z]|\\)?(\w([''-]\w)?)+|(\(|\)|-|""|'')");
        Regex negationDetection = new Regex(@"\bcannot|(ai|are|ca|could|did|does|do|had|has|have|is|must|need|ought|shall|should|was|were|wo|would)n'?t\b");
        Regex emojiDetection;


        public Tokenizer(List<Emoji> emojisToDetect)
        {
            initEmojiRegex(emojisToDetect);
        }

        private void initEmojiRegex(List<Emoji> emojis)
        {
            string regexString = $"{emojis.First().unicodeCharacter}";
            foreach(Emoji emoji in emojis.Skip(1))
            {
                regexString += $"|{emoji.unicodeCharacter}";
            }
            emojiDetection = new Regex(regexString);
        }

        public List<Token> splitIntoTokens(Tweet tweet)
        {
            List<Token> allTokens = new List<Token>();
            int tokenIndex = 0;

            tweet.fullText = tweet.fullText.Replace(TokenPartConstants.NEW_LINE, TokenPartConstants.SPACE);

            MatchCollection tokenMatches = tokenDetection.Matches(tweet.fullText);

            foreach(Match match in tokenMatches)
            {
                string text = match.Value;
                MatchCollection negationMatches = negationDetection.Matches(text);
                if (negationMatches.Count > 0)
                {
                    string firstPart;
                    string secondPart;
                    if (text.EndsWith(TokenPartConstants.NEGATION_TOKEN_ENDING_WITHOUT_APOSTROPHE))
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
                else if(tweet.fullText.Contains(TokenPartConstants.APOSTROPHE))
                {
                    string[] parts = text.Split(TokenPartConstants.APOSTROPHE);
                    for(int i = 0; i < parts.Length; i++)
                    {
                        Token token;
                        if (i > 0)
                            token = new Token(TokenPartConstants.APOSTROPHE + parts[i], tokenIndex);
                        else
                            token = new Token(parts[i], tokenIndex);
                        allTokens.Add(token);
                        tokenIndex++;
                    }
                    continue;
                }
                Token t = new Token(text, tokenIndex);
                tokenIndex++;
                allTokens.Add(t);
            }

            return allTokens;
        }
    }
}
