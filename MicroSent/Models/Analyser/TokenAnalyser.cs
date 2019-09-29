using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class TokenAnalyser
    {
        private const string Hashtag = "#";
        private const string Mention = "@";

        private const string HunspellDataPath = @".\data\nhunspell\";

        private Dictionary<string, string> abbreviations = new Dictionary<string, string>();
        private NHunspell.Hunspell hunspell;

        public TokenAnalyser()
        {
            abbreviations.Add("r", "are");
            abbreviations.Add("u", "you");
            abbreviations.Add("y", "why"); // need papers for this

            hunspell = new NHunspell.Hunspell($"{HunspellDataPath}en_us.aff", $"{HunspellDataPath}en_us.dic");
        }

        public void analyseTokenType(ref Token token)
        {
            if (checkForHashtag(ref token))
            {
                return;
            }
            else if (checkForMention(ref token))
            {
                return;
            }
            else if (checkForLink(ref token))
            {
                return;
            }
            else if (checkForPunctuation(ref token))
            {
                return;
            }
            else if (checkForSentenceStructure(ref token))
            {
                return;
            }
            else if (checkForSmiley(ref token))
            {
                return;
            }
            else if (checkForEmoticon(ref token))
            {
                return;
            }
            else if (checkForLaughingExpression(ref token))
            {
                return;
            }
        }

        #region tokentype
        private bool checkForHashtag(ref Token token)
        {
            if (token.textBeforeSplittingIntoSubTokens.StartsWith(Hashtag))
            {
                token.textBeforeSplittingIntoSubTokens = token.textBeforeSplittingIntoSubTokens.Remove(0, 1);
                //analyseHashtag
                return token.isHashtag = true;
            }
            return false;
        }

        private bool checkForMention(ref Token token)
        {
            if (token.textBeforeSplittingIntoSubTokens.StartsWith(Mention))
            {
                token.textBeforeSplittingIntoSubTokens = token.textBeforeSplittingIntoSubTokens.Remove(0, 1);
                return token.isMention = true;
            }
            return false;
        }

        private bool checkForLink(ref Token token)
        {
            Regex linkRegex = new Regex(@"(https:\/\/(www\.)?|www\.)([\d\w]+[\.\/])+[\d\w\?\=]+");
            MatchCollection linkMatches = linkRegex.Matches(token.textBeforeSplittingIntoSubTokens);

            if (linkMatches.Count > 0)
            {
                return token.isLink = true;
            }
            return false;
        }

        private bool checkForPunctuation(ref Token token)
        {
            Regex puntuationRegex = new Regex(@"([\?!]+|\.+|,|:)");
            MatchCollection punktuationMatches = puntuationRegex.Matches(token.textBeforeSplittingIntoSubTokens);

            if (punktuationMatches.Count > 0)
            {
                return token.isPunctuation = true;
            }
            return false;
        }

        private bool checkForSentenceStructure(ref Token token)
        {
            Regex sentenceStructureRegex = new Regex(@"(\(|\)|-)");
            MatchCollection sentenceStructureMatches = sentenceStructureRegex.Matches(token.textBeforeSplittingIntoSubTokens);

            if (sentenceStructureMatches.Count > 0)
            {
                return token.isStructureToken = true;
            }
            return false;
        }

        private bool checkForSmiley(ref Token token)
        {
            Regex smileyRegex = new Regex(@"((:-?|=)(\)|\(|\||\/|(D\b))|\bD:|:\s[\)\(])");
            MatchCollection smileyMatches = smileyRegex.Matches(token.textBeforeSplittingIntoSubTokens);

            if (smileyMatches.Count > 0)
            {
                return token.isSmiley = true;
            }
            return false;
        }

        private bool checkForEmoticon(ref Token token)
        {
            Regex emoticonRegex = new Regex(@"\\U[a-f0-9]{4,8}");
            MatchCollection emoticonMatches = emoticonRegex.Matches(token.textBeforeSplittingIntoSubTokens);

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
            MatchCollection hahaMatches = hahaRegex.Matches(token.textBeforeSplittingIntoSubTokens);
            MatchCollection hihiMatches = hihiRegex.Matches(token.textBeforeSplittingIntoSubTokens);

            if (hahaMatches.Count > 0 || hihiMatches.Count > 0)
            {
                return token.isLaughingExpression = true;
            }
            return false;
        }
        #endregion

        public void checkForUppercase(ref Token token)
        {
            bool isAllUppercase = false;
            for (int j = 0; j < token.subTokens.Count; j++)
            {
                SubToken subToken = token.subTokens[j];

                if (subToken.text == "I")
                    return;

                foreach (char letter in subToken.text)
                {
                    if (!char.IsUpper(letter))
                    {
                        return;
                    }
                }

                subToken.text = subToken.text.ToLower();
                isAllUppercase = true;
                token.subTokens[j] = subToken;
            }
            token.isAllUppercase = isAllUppercase;
        }

        public void replaceAbbreviations(ref Token token)
        {
            //TODO: redo this
        }

        public void removeRepeatedLetters(ref Token token)
        {
            for (int j = 0; j < token.subTokens.Count; j++)
            {
                SubToken subToken = token.subTokens[j];
                for (int i = 2; i < subToken.text.Length; i++)
                {
                    char currentLetter = subToken.text[i];
                    char lastLetter = subToken.text[i - 1];
                    char secondLastLetter = subToken.text[i - 2];

                    if (currentLetter == lastLetter && currentLetter == secondLastLetter)
                    {
                        token.hasRepeatedLetters = true;
                        subToken.text = subToken.text.Remove(i, 1);
                        i--;
                    }
                }

                if(subToken.text != token.subTokens[j].text)
                {
                    token.subTokens[j] = subToken;
                }
            }
        }

        public void splitToken(ref Token token)
        {
            if (token.isHashtag)
            {
                splitHashtag(ref token);
                return;
            }

            splitWord(ref token);
        }

        private void splitWord(ref Token token)
        {
            List<string> singleWords = token.textBeforeSplittingIntoSubTokens.Split(" ").ToList();
            for (int i = 0; i < singleWords.Count; i++)
            {
                string word = singleWords[i];

                Tuple<string, string> negationWordParts = getSplitIfNegationWord(word);

                if(negationWordParts != null)
                {
                    singleWords[i] = negationWordParts.Item1;
                    singleWords.Insert(i + 1, negationWordParts.Item2);
                }
            }
            token.subTokens.AddRange(generateSubTokens(singleWords));
        }

        private Tuple<string, string> getSplitIfNegationWord(string word)
        {
            Regex negationWord = new Regex(@"\bcannot|(ai|are|ca|could|did|does|do|had|has|have|is|must|need|ought|shall|should|was|were|wo|would)n'?t\b");
            Match match = negationWord.Match(word);
            if (match.Success)
            {
                string[] parts = new string[2];
                Tuple<string, string> splitWord;
                if (word.EndsWith("nt"))
                {
                    splitWord = new Tuple<string, string>(word.Substring(0, word.Length - 2), word.Substring(word.Length - 2));
                }
                else
                {
                    splitWord = new Tuple<string, string>(word.Substring(0, word.Length - 3), word.Substring(word.Length - 3));
                }
                return splitWord;
            }
            return null;
        }

        #region hashtag parsing
        private void splitHashtag(ref Token token)
        {
            string hashtag = token.textBeforeSplittingIntoSubTokens;


            Console.WriteLine("Forward parsing:");
            Tuple<bool, List<string>> forwardTuple = parseHashtagForward(hashtag);

            //notverynice
            Console.WriteLine("Backward parsing:");
            Tuple<bool, List<string>> backwardTuple = parseHashtagBackwards(hashtag);

            setBetterSubTokenList(ref token, forwardTuple, backwardTuple);
        }

        private void setBetterSubTokenList(ref Token token, Tuple<bool, List<string>> forwardTuple, Tuple<bool, List<string>> backwardTuple)
        {
            List<string> forwardParsingList = forwardTuple.Item2;
            List<string> backwardParsingList = backwardTuple.Item2;
            bool lastForwardProcessedWordMakesSense = forwardTuple.Item1;
            bool lastBackwardProcessedWordMakesSense = backwardTuple.Item1;

            if (lastForwardProcessedWordMakesSense)
            {
                if (lastBackwardProcessedWordMakesSense)
                {
                    //both succeded
                    token.subTokens.AddRange(generateSubTokens(backwardParsingList));
                }
                else
                {
                    //forward processing succeded
                    token.subTokens.AddRange(generateSubTokens(forwardParsingList));
                }
            }
            else
            {
                if (lastBackwardProcessedWordMakesSense)
                {
                    //backward processing succeded
                    token.subTokens.AddRange(generateSubTokens(backwardParsingList));
                }
                else
                {
                    //both failed
                    //longer list means more correct tokens
                    List<string> longerList = forwardParsingList.Count > backwardParsingList.Count ? forwardParsingList : backwardParsingList;

                    token.subTokens.AddRange(generateSubTokens(longerList));
                }
            }
        }

        private Tuple<bool, List<string>> parseHashtagBackwards(string hashtag)
        {
            string restToAnalyse = hashtag;
            string currentWord = hashtag;
            List<string> backwardParsingList = new List<string>();
            bool lastBackwardProcessedWordMakesSense = true;

            while (restToAnalyse.Length > 0)
            {
                if (currentWord.Length == 0)
                {
                    Console.WriteLine($"Rest: {restToAnalyse}");
                    lastBackwardProcessedWordMakesSense = false;
                    break;
                }

                if (hunspell.Spell(currentWord))
                {
                    Console.WriteLine($"{currentWord} is part of {hashtag}");
                    backwardParsingList.Insert(0, currentWord);
                    restToAnalyse = restToAnalyse.Substring(0, restToAnalyse.Length - currentWord.Length);
                    currentWord = restToAnalyse;
                }
                else
                {
                    currentWord = currentWord.Substring(1);
                }
            }
            return new Tuple<bool, List<string>>(lastBackwardProcessedWordMakesSense, backwardParsingList);
        }

        private Tuple<bool, List<string>> parseHashtagForward(string hashtag)
        {
            string restToAnalyse = hashtag;
            string currentWord = hashtag;
            List<string> forwardParsingList = new List<string>();
            bool lastForwardProcessedWordMakesSense = true;

            while (restToAnalyse.Length > 0)
            {
                if (currentWord.Length == 0)
                {
                    Console.WriteLine($"Rest: {restToAnalyse}");
                    lastForwardProcessedWordMakesSense = false;
                    break;
                }

                if (hunspell.Spell(currentWord))
                {
                    Console.WriteLine($"{currentWord} is part of {hashtag}");
                    forwardParsingList.Add(currentWord);
                    restToAnalyse = restToAnalyse.Substring(currentWord.Length);
                    currentWord = restToAnalyse;
                }
                else
                {
                    currentWord = currentWord.Substring(0, currentWord.Length - 2);
                }
            }
            return new Tuple<bool, List<string>>(lastForwardProcessedWordMakesSense, forwardParsingList);
        }
        #endregion

        private List<SubToken> generateSubTokens(List<string> subTokenWords)
        {
            List<SubToken> subTokens = new List<SubToken>();
            for(int i = 0; i< subTokenWords.Count; i++)
            {
                subTokens.Add(new SubToken(subTokenWords[i], i));
            }
            return subTokens;
        }
    }
}
