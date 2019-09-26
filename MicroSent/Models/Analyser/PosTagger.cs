using MicroSent.Models.Enums;
using OpenNLP.Tools.Parser;
using OpenNLP.Tools.PosTagger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models.Analyser
{
    public class PosTagger
    {
        private EnglishMaximumEntropyPosTagger nlpPosTagger;
        private EnglishTreebankParser nlpParser;
        private string nbinFilePath = @"data\NBIN_files\";

        public PosTagger()
        {
            nlpPosTagger = new EnglishMaximumEntropyPosTagger(nbinFilePath + "EnglishPOS.nbin", nbinFilePath + "tagdict");
            nlpParser = new EnglishTreebankParser(nbinFilePath, true, false);
        }

        public void cutIntoSentences(ref Tweet tweet)
        {
            int sentenceIndex = 0;
            int sentenceTokensAmount = 0;
            for (int i = 0; i < tweet.allTokens.Count; i++)
            {
                Token token = tweet.allTokens[i];
                token.sentenceIndex = sentenceIndex;
                token.indexInSentence = sentenceTokensAmount;
                tweet.allTokens[i] = token;

                Token currentToken = tweet.allTokens[i];
                sentenceTokensAmount++;
                if (currentToken.isPunctuation && currentToken.text != ",")
                {

                    sentenceTokensAmount = 0;
                    sentenceIndex++;
                }
            }
            tweet.sentenceCount = sentenceIndex + 1;
        }

        public void parseTweet(ref Tweet tweet)
        {
            if (tweet.sentenceCount == 0)
            {
                return;
            }

            int sentenceIndex = 0;
            List<Token> sentenceTokens = new List<Token>();

            for (int tokenIndex = 0; tokenIndex < tweet.allTokens.Count; tokenIndex++) //each (Token token in tweet.allTokens)
            {
                Token token = tweet.allTokens[tokenIndex];
                sentenceTokens.Add(token);

                if (tokenIndex + 1 < tweet.allTokens.Count
                    && tweet.allTokens[tokenIndex + 1].sentenceIndex != sentenceIndex)
                {
                    var tags = nlpPosTagger.Tag(sentenceTokens.Select(t => t.text).ToArray());
                    var parse = nlpParser.DoParse(sentenceTokens.Select(t => t.text).ToArray());
                    tweet.parseTrees.Add(parse.GetChildren()[0]);

                    for (int tagIndex = 0; tagIndex < tags.Length; tagIndex++)
                    {
                        //translate the tag into PosLabels enum
                        int tokenPosition = sentenceTokens[tagIndex].indexInTweet;
                        if (Enum.TryParse(tags[tagIndex], out PosLabels label))
                        {
                            Token t = tweet.allTokens[tokenPosition];
                            t.posLabel = label;
                            tweet.allTokens[tokenPosition] = t;
                        }
                    }
                    sentenceTokens.Clear();
                    sentenceIndex++;
                }
            }
        }
    }
}
