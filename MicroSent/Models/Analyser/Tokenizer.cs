using MicroSent.Models.Constants;
using MicroSent.Models.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class Tokenizer
    {
        Regex tokenDetection;
        Regex negationDetection;

        public Tokenizer()
        {
            initRegex();
        }

        private void initRegex()
        {
            tokenDetection = new Regex($"({RegexConstants.LINK_DETECTION})" +
                $"|({RegexConstants.SMILEY_DETECTION})" +
                $"|({RegexConstants.PUNCTUATION_DETECTION})" +
                $"|({RegexConstants.WORDS_DETECTION})" +
                $"|({RegexConstants.SENTENCE_STRUCTURE_DETECTION})" +
                $"|({RegexConstants.ALL_EMOTICON_DETECTION})");
            negationDetection = new Regex($"{RegexConstants.NEGATION_WORD_DETECTION}");
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
