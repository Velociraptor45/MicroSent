using MicroSent.Models.Constants;
﻿using MicroSent.Models.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class TweetAnalyser
    {
        private const string IronyString = "irony";
        private const string SarcasmStringSlash = "/s";
        private const string SarcasmStringBackslash = "\\s";
        
        private Regex negationToken = new Regex(RegexConstants.NEGATION_TOKEN_PATTERN);
        private Regex negationHashtagPart = new Regex(RegexConstants.NEGATION_HASHTAG_PATTERN);

        private List<string> whWords = new List<string> { "what", "where", "when", "why", "who" };
        private List<string> auxiliaryVerbs = new List<string> { "am", "is", "are", "was", "were", "do", "did", "does" };

        private Regex domainDetection = new Regex(RegexConstants.DOMAIN_PATTERN);

        public TweetAnalyser()
        {

        }

        public void analyseFirstEndHashtagPosition(List<Token> tokens, Tweet tweet)
        {
            for(int i = tokens.Count - 1; i > 0; i--)
            {
                Token currentToken = tokens[i];
                Token previousToken = tokens[i - 1];
                if (currentToken.isLink)
                {
                    continue;
                }
                else if(currentToken.isHashtag)
                {
                    if (!previousToken.isHashtag)
                    {
                        tweet.firstEndHashtagIndex = currentToken.indexInTweet;
                    }
                    else
                    {
                        continue;
                    }
                }
                break;
            }
        }

        public void checkforIrony(Tweet tweet)
        {
            tweet.isIronic = hasIronyEndHashtag(tweet) || hasSarcasmToken(tweet);
        }

        private bool hasIronyEndHashtag(Tweet tweet)
        {
            if (tweet.firstEndHashtagIndex == -1)
                return false;

            var endHastags = tweet.rest.Where(t => t.indexInTweet >= tweet.firstEndHashtagIndex && t.isHashtag);
            foreach (Token token in endHastags)
            {
                if(token.text == IronyString)
                {
                    return true;
                }
            }

            return false;
        }

        private bool hasSarcasmToken(Tweet tweet)
        {
            if (tweet.sentences.Count == 0)
                return false;

            foreach(Token token in tweet.sentences.Last())
            {
                if (token.text == SarcasmStringSlash
                    || token.text == SarcasmStringBackslash)
                    return true;
            }
            return false;
        }

        public string extractDomain(string url)
        {
            string fullHost = new Uri(url).Host;
            return domainDetection.Match(fullHost).Value;
        }


        #region special structure filtering
        public void filterUselessInterogativeSentences(Tweet tweet)
        {
            foreach(var sentence in tweet.sentences)
            {
                if(sentence.Count > 3)
                {
                    Token firstToken = sentence.First();
                    Token secondToken = sentence[1];
                    Token lastToken = sentence.Last();
                    
                    if(isWhWord(firstToken)
                        && isAuxiliaryVerb(secondToken)
                        && lastToken.text.Contains(TokenPartConstants.QUESTIONMARK))
                    {
                        ConsolePrinter.printSentenceIgnored(tweet, tweet.sentences.IndexOf(sentence));
                        ignoreSentenceForRating(sentence);
                    }
                }
            }
        }

        private bool isWhWord(Token token)
        {
            return whWords.Contains(token.text);
        }

        private bool isAuxiliaryVerb(Token token)
        {
            return auxiliaryVerbs.Contains(token.text);
        }

        private void ignoreSentenceForRating(List<Token> sentence)
        {
            foreach (Token token in sentence)
            {
                token.ignoreInRating = true;
            }
        }
        #endregion

        #region negation word detection
        //////////////////////////////////////////////////////////////////////////
        /// NEGATION WORD DETECTION
        //////////////////////////////////////////////////////////////////////////

        private List<int> getSentenceIndexesOfNegationWord(List<Token> sentenceTokens, Tweet tweet)
        {
            List<int> tokenIndexes = new List<int>();
            foreach (Token token in sentenceTokens)
            {
                MatchCollection matches = negationToken.Matches(token.text);
                if(matches.Count > 0)
                {
                    tokenIndexes.Add(token.indexInSentence);
                }
            }
            return tokenIndexes;
        }
        #endregion

        #region negation application
        //////////////////////////////////////////////////////////////////////////
        /// NEGATION APPLICATION
        //////////////////////////////////////////////////////////////////////////

        public void applySpecialStructureNegation(Tweet tweet)
        {
            foreach(var sentence in tweet.sentences)
            {
                for(int i = 0; i < sentence.Count; i++)
                {
                    negateGerundForms(i, sentence);
                    negateBaseFroms(i, sentence);
                }
            }
        }

        private void negateGerundForms(int tokenIndex, List<Token> sentence)
        {
            Token token = sentence[tokenIndex];
            if ((token.text == "stop" || token.text == "quit" || token.stemmedText == "stop" || token.stemmedText == "quit")
                        && token.indexInSentence < sentence.Count - 1)
            {
                Token nextToken = sentence[tokenIndex + 1];
                if (nextToken.posLabel == Enums.PosLabels.VBG)
                {
                    negate(nextToken);
                }
            }
        }

        private void negateBaseFroms(int tokenIndex, List<Token> sentence)
        {
            Token token = sentence[tokenIndex];
            if ((token.text == "cease" || token.stemmedText == "ceas")
                && token.indexInSentence < sentence.Count - 2)
            {
                Token nextToken = sentence[tokenIndex + 1];
                Token secondNextToken = sentence[tokenIndex + 2];

                if (nextToken.text == "to" && secondNextToken.posLabel == Enums.PosLabels.VB)
                {
                    negate(secondNextToken);
                }
            }
        }

        public void applyKWordNegation(Tweet tweet, int negatedWordDistance,
            bool negateLeftSide = true, bool negateRightSide = true, bool ignoreSentenceBoundaries = false)
        {
            foreach (List<Token> sentenceTokens in tweet.sentences)
            {
                foreach (int tokenSentenceIndex in getSentenceIndexesOfNegationWord(sentenceTokens, tweet))
                {
                    Token token = sentenceTokens[tokenSentenceIndex];

                    int firstNegationIndex = negateLeftSide ? token.indexInTweet - negatedWordDistance : token.indexInTweet;
                    int lastNegationIndex = negateRightSide ? token.indexInTweet + negatedWordDistance : token.indexInTweet;

                    //the first part of the negation word is eg. do, did, could, ...
                    //this counts as token but shouldn't, so the negation distance must be increased
                    if (token.text == TokenPartConstants.NEGATION_TOKEN_ENDING_WITH_APOSTROPHE|| token.text == TokenPartConstants.NEGATION_TOKEN_ENDING_WITHOUT_APOSTROPHE)
                        firstNegationIndex--;

                    //tweet boundaries
                    firstNegationIndex = firstNegationIndex < 0 ? 0 : firstNegationIndex;
                    lastNegationIndex = lastNegationIndex > tweet.tokenCount - 1 ? tweet.tokenCount - 1 : lastNegationIndex;

                    //sentence boundaries
                    if (!ignoreSentenceBoundaries)
                    {
                        if (sentenceTokens.Count(t => t.indexInTweet == firstNegationIndex) == 0)
                        {
                            firstNegationIndex = sentenceTokens.First().indexInTweet;
                        }
                        while (sentenceTokens.Count(t => t.indexInTweet == lastNegationIndex) == 0)
                        {
                            lastNegationIndex = sentenceTokens.Last().indexInTweet;
                        }
                    }

                    for (int i = firstNegationIndex; i <= lastNegationIndex; i++)
                    {
                        if (i == tokenSentenceIndex)
                        {
                            continue;
                        }
                        negate(tweet.getTokenByIndex(i));
                    }
                }
            }
        }

        public void applyEndHashtagNegation(Tweet tweet)
        {
            if (tweet.firstEndHashtagIndex == -1)
                return;

            foreach(Token token in tweet.rest.Where(t => t.indexInTweet >= tweet.firstEndHashtagIndex))
            {
                if (token.isHashtag)
                {
                    foreach (SubToken subToken in token.subTokens)
                    {
                        Match match = negationHashtagPart.Match(subToken.text);
                        if (match.Success)
                        {
                            negate(token);
                            break;
                        }
                    }
                }
            }
        }

        public void applyParseTreeDependentNegation(Tweet tweet, bool negationBeforeQuestionmark)
        {
            for(int sentenceIndex = 0; sentenceIndex < tweet.sentences.Count; sentenceIndex++)
            {
                List<Token> sentenceTokens = tweet.sentences[sentenceIndex];
                if (!negationBeforeQuestionmark && sentenceTokens.Last().text == (TokenPartConstants.QUESTIONMARK).ToString())
                {
                    continue;
                }

                List<int> negationWordIndexes = getSentenceIndexesOfNegationWord(sentenceTokens, tweet);

                foreach(int negationWordIndex in negationWordIndexes)
                {
                    List<int> tokenSentenceIndexesToNegate = getNegationRangeIndexes(tweet.parseTrees[sentenceIndex], negationWordIndex);
                    foreach(int tokenSentenceIndexToNegate in tokenSentenceIndexesToNegate)
                    {
                        Token token = sentenceTokens.Where(t => t.indexInSentence == tokenSentenceIndexToNegate).ToList().FirstOrDefault();
                        if (token.indexInTweet > -1)
                        {
                            negate(token);
                        }
                        else
                        {
                            ConsolePrinter.printSentenceNotFoundMessage(sentenceIndex, tokenSentenceIndexToNegate);
                        }
                    }
                }
            }
        }

        private void negate(Token token)
        {
            token.negationRating *= RatingConstants.NEGATION;
        }
        #endregion

        #region tree parsing for negation
        //////////////////////////////////////////////////////////////////////////
        /// TREE PARSING FOR NEGATION
        //////////////////////////////////////////////////////////////////////////

        private List<int> getNegationRangeIndexes(Node root, int negationWordSentenceIndex)
        {
            List<int> indexRange = new List<int>();
            depthSearch(root, negationWordSentenceIndex, indexRange);

            return indexRange;
        }

        private Tuple<int, int> depthSearch(Node node, int negationWordSentenceIndex, List<int> allIndexes)
        {
            int smallestChildrenIndex = int.MaxValue;
            int highestChildrenIndex = int.MinValue;
            
            if (node.children.Count == 0)
            {
                int thisTokenIndex = node.correspondingToken.indexInSentence;
                return new Tuple<int, int>(thisTokenIndex, thisTokenIndex);
            }
            else
            {
                foreach (var child in node.children)
                {
                    var indexRange = depthSearch(child, negationWordSentenceIndex, allIndexes);
                    if(smallestChildrenIndex > indexRange.Item1)
                        smallestChildrenIndex = indexRange.Item1;
                    if(highestChildrenIndex < indexRange.Item2)
                        highestChildrenIndex = indexRange.Item2;
                }
            }

            if (allIndexes.Count == 0
                && smallestChildrenIndex <= negationWordSentenceIndex && highestChildrenIndex >= negationWordSentenceIndex)
            {
                int amount = highestChildrenIndex - smallestChildrenIndex + 1;
                allIndexes.AddRange(Enumerable.Range(smallestChildrenIndex, amount));
            }

            return new Tuple<int, int>(smallestChildrenIndex, highestChildrenIndex);
        }
        #endregion
    }
}
