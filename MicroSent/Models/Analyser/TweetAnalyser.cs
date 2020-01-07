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
        #region private members
        private const string IronyString = "irony";
        private const string SarcasmStringSlash = "/s";
        private const string SarcasmStringBackslash = "\\s";

        private List<string> whWords = new List<string> { "what", "where", "when", "why", "who" };
        private List<string> auxiliaryVerbs = new List<string> { "am", "is", "are", "was", "were", "do", "did", "does" };

        private Regex domainDetection = new Regex(RegexConstants.DOMAIN_PATTERN);
        #endregion

        #region constructors
        public TweetAnalyser()
        {

        }
        #endregion

        #region public methods
        public void analyseFirstEndHashtagPosition(List<Token> tokens, Tweet tweet)
        {
            for (int i = tokens.Count - 1; i > 0; i--)
            {
                Token currentToken = tokens[i];
                Token previousToken = tokens[i - 1];
                if (currentToken.isLink)
                {
                    continue;
                }
                else if (currentToken.isHashtag)
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

        public void filterUselessInterogativeSentences(Tweet tweet)
        {
            foreach (var sentence in tweet.sentences)
            {
                if (sentence.Count > 3)
                {
                    Token firstToken = sentence.First();
                    Token secondToken = sentence[1];
                    Token lastToken = sentence.Last();

                    if (isWhWord(firstToken)
                        && isAuxiliaryVerb(secondToken)
                        && lastToken.text.Contains(TokenPartConstants.QUESTIONMARK))
                    {
                        ConsolePrinter.printSentenceIgnored(tweet, tweet.sentences.IndexOf(sentence));
                        ignoreSentenceForRating(sentence);
                    }
                }
            }
        }

        public string extractDomain(string url)
        {
            string fullHost = new Uri(url).Host;
            return domainDetection.Match(fullHost).Value;
        }
        #endregion

        #region private methods
        private bool hasIronyEndHashtag(Tweet tweet)
        {
            if (tweet.firstEndHashtagIndex == -1)
                return false;

            var endHastags = tweet.rest.Where(t => t.indexInTweet >= tweet.firstEndHashtagIndex && t.isHashtag);
            foreach (Token token in endHastags)
            {
                if (token.text == IronyString)
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

            foreach (Token token in tweet.sentences.Last())
            {
                if (token.text == SarcasmStringSlash
                    || token.text == SarcasmStringBackslash)
                    return true;
            }
            return false;
        }

        #region special structure filtering
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
        #endregion
    }
}
