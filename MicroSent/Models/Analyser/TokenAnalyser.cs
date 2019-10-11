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

        public void analyseTokenType(Token token)
        {
            if (checkForHashtag(token))
            {
                return;
            }
            else if (checkForMention(token))
            {
                return;
            }
            else if (checkForLink(token))
            {
                return;
            }
            else if (checkForPunctuation(token))
            {
                return;
            }
            else if (checkForSentenceStructure(token))
            {
                return;
            }
            else if (checkForSmiley(token))
            {
                return;
            }
            else if (checkForEmoticon(token))
            {
                return;
            }
            else if (checkForLaughingExpression(token))
            {
                return;
            }
        }

        #region tokentype
        private bool checkForHashtag(Token token)
        {
            if (token.text.StartsWith(Hashtag))
            {
                token.text = token.text.Remove(0, 1);
                //analyseHashtag
                return token.isHashtag = true;
            }
            return false;
        }

        private bool checkForMention(Token token)
        {
            if (token.text.StartsWith(Mention))
            {
                token.text = token.text.Remove(0, 1);
                return token.isMention = true;
            }
            return false;
        }

        private bool checkForLink(Token token)
        {
            Regex linkRegex = new Regex(@"(https?:\/\/(www\.)?|www\.)([\d\w]+[\.\/])+[\d\w\?\=]+");
            MatchCollection linkMatches = linkRegex.Matches(token.text);

            if (linkMatches.Count > 0)
            {
                return token.isLink = true;
            }
            return false;
        }

        private bool checkForPunctuation(Token token)
        {
            Regex puntuationRegex = new Regex(@"([\?!]+|\.+|,|:)");
            MatchCollection punktuationMatches = puntuationRegex.Matches(token.text);

            if (punktuationMatches.Count > 0)
            {
                return token.isPunctuation = true;
            }
            return false;
        }

        private bool checkForSentenceStructure(Token token)
        {
            Regex sentenceStructureRegex = new Regex(@"(\(|\)|-)");
            MatchCollection sentenceStructureMatches = sentenceStructureRegex.Matches(token.text);

            if (sentenceStructureMatches.Count > 0)
            {
                return token.isStructureToken = true;
            }
            return false;
        }

        private bool checkForSmiley(Token token)
        {
            Regex smileyRegex = new Regex(@"((:-?|=)(\)|\(|\||\/|(D\b))|\bD:|:\s[\)\(])");
            MatchCollection smileyMatches = smileyRegex.Matches(token.text);

            if (smileyMatches.Count > 0)
            {
                return token.isSmiley = true;
            }
            return false;
        }

        private bool checkForEmoticon(Token token)
        {
            Regex emoticonRegex = new Regex(@"\\U[a-f0-9]{4,8}");
            MatchCollection emoticonMatches = emoticonRegex.Matches(token.text);

            if (emoticonMatches.Count > 0)
            {
                return token.isEmoticon = true;
            }
            return false;
        }

        private bool checkForLaughingExpression(Token token)
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
        #endregion

        public void checkForUppercase(Token token)
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

            token.isAllUppercase = true;
        }

        public void convertToLowercase(Token token)
        {
            token.text = token.text.ToLower();
            foreach(SubToken subToken in token.subTokens)
            {
                subToken.text = subToken.text.ToLower();
            }
        }

        #region repeated Letters
        public void removeRepeatedLetters(Token token)
        {
            cutOutRepeatedLetters(token, out List<int> firstIndexesOfRepeatSections);

            if (!hunspell.Spell(token.text))
            {
                string text = token.text;
                string analysedWord = findEnglishWordFromRepeatedLetters(text, firstIndexesOfRepeatSections);
                if(analysedWord != null)
                {
                    token.text = analysedWord;
                }
            }
        }

        private void cutOutRepeatedLetters(Token token, out List<int> firstIndexesOfRepeatSections)
        {
            firstIndexesOfRepeatSections = new List<int>();
            int sectionStartIndex = -1;

            for (int i = 2; i < token.text.Length; i++)
            {
                char currentLetter = token.text[i];
                char lastLetter = token.text[i - 1];
                char secondLastLetter = token.text[i - 2];

                if (currentLetter == lastLetter && currentLetter == secondLastLetter)
                {
                    sectionStartIndex = i - 2;
                    token.hasRepeatedLetters = true;
                    token.text = token.text.Remove(i, 1);
                    i--;
                }
                else if (sectionStartIndex != -1)
                {
                    firstIndexesOfRepeatSections.Add(sectionStartIndex);
                    sectionStartIndex = -1;
                }
            }

            // if repeated letters are at the end of the word,
            // the sectionStartIndex must be saved manually
            // because the for-loop was exited already
            if (sectionStartIndex != -1)
            {
                firstIndexesOfRepeatSections.Add(sectionStartIndex);
            }
        }

        private string findEnglishWordFromRepeatedLetters(string originalText, List<int> indexesToCut)
        {
            if(indexesToCut.Count == 0)
            {
                if (hunspell.Spell(originalText))
                {
                    return originalText;
                }
                else
                {
                    return null;
                }
            }

            int lastListIndex = indexesToCut.Count - 1;
            List<int> updatedIndexesToCut = new List<int>(indexesToCut);
            updatedIndexesToCut.RemoveAt(lastListIndex);
            string removedLetterText = originalText.Remove(indexesToCut[lastListIndex], 1);

            string valueRemovedLetter = findEnglishWordFromRepeatedLetters(removedLetterText, updatedIndexesToCut);
            string valueNotRemovedLetter = findEnglishWordFromRepeatedLetters(originalText, updatedIndexesToCut);

            return valueRemovedLetter ?? valueNotRemovedLetter;
        }
        #endregion
        
        #region hashtag parsing
        public void splitHashtag(Token token)
        {
            string hashtag = token.text;

            Console.WriteLine("----------------- start analysis --------------------------");
            Console.WriteLine("Forward parsing:");
            Tuple<bool, List<string>> forwardTuple = parseHashtagForward(hashtag);

            Console.WriteLine("Backward parsing:");
            Tuple<bool, List<string>> backwardTuple = parseHashtagBackwards(hashtag);

            setBetterSubTokenList(token, forwardTuple, backwardTuple);
        }

        private void setBetterSubTokenList(Token token, Tuple<bool, List<string>> forwardTuple, Tuple<bool, List<string>> backwardTuple)
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
