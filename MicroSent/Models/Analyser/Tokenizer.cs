using MicroSent.Models.Constants;
using MicroSent.Models.Serialization;
using MicroSent.Models.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class Tokenizer
    {
        #region private members
        Regex tokenDetection;
        Regex negationDetection;
        #endregion

        #region constructors
        public Tokenizer()
        {
            initRegex();
        }
        #endregion

        #region public methods
        public List<Token> splitIntoTokens(Tweet tweet)
        {
            List<Token> allTokens = new List<Token>();
            int tokenIndex = 0;

            tweet.fullText = tweet.fullText.Replace(TokenPartConstants.NEW_LINE, TokenPartConstants.SPACE);

            MatchCollection tokenMatches = tokenDetection.Matches(tweet.fullText);

            foreach (Match match in tokenMatches)
            {
                string text = match.Value;

                //the regex code finds a unicode character called "emoji variation selector" by doing a normal \w+
                //this must be sorted out because the google parser can't handle it
                if (UnicodeHelper.isEmojiVariationSelector(text))
                    continue;

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
                else if (tweet.fullText.Contains(TokenPartConstants.APOSTROPHE))
                {
                    string[] parts = text.Split(TokenPartConstants.APOSTROPHE);
                    for (int i = 0; i < parts.Length; i++)
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
        #endregion

        #region private methods
        private void initRegex()
        {
            tokenDetection = new Regex($"({RegexConstants.LINK_PATTERN})" +
                $"|({RegexConstants.ALL_SMILEY_PATTERN})" +
                $"|({RegexConstants.SENTENCE_STRUCTURE_PATTERN})" +
                $"|({RegexConstants.PUNCTUATION_PATTERN})" +
                $"|({RegexConstants.WORDS_PATTERN})" +
                $"|({RegexConstants.ALL_EMOJI_PATTERN})");
            negationDetection = new Regex($"{RegexConstants.NEGATION_WORD_PATTERN}");
        }
        #endregion
    }
}
