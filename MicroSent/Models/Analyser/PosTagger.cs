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
                Token currentToken = tweet.allTokens[i];
                if (sentenceIndex == 0 && currentToken.isLink)
                {
                    continue;
                }
                if(tweet.firstEndHashtagIndex != -1 && currentToken.subTokens.First().indexInTweet >= tweet.firstEndHashtagIndex)
                {
                    break;
                }

                currentToken.sentenceIndex = sentenceIndex;
                for (int j = 0; j < currentToken.subTokens.Count; j++)
                {
                    SubToken subToken = currentToken.subTokens[j];
                    subToken.indexInSentence = sentenceTokensAmount;

                    currentToken.subTokens[j] = subToken;
                    sentenceTokensAmount++;
                }
                tweet.allTokens[i] = currentToken;

                if (currentToken.isPunctuation && currentToken.textBeforeSplittingIntoSubTokens != ",")
                {
                    tweet.lastTokenIndexInSentence.Add(sentenceIndex, currentToken.subTokens.Last().indexInTweet);
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

            for (int tokenIndex = 0; tokenIndex < tweet.allTokens.Count; tokenIndex++)
            {
                Token token = tweet.allTokens[tokenIndex];
                sentenceTokens.Add(token);

                if ((tokenIndex + 1 < tweet.allTokens.Count -1 && tweet.allTokens[tokenIndex + 1].sentenceIndex != sentenceIndex)
                    || tokenIndex == tweet.allTokens.Count - 1)
                {
                    fillWithAllSubTokensAsText(out List<string> allSentenceSubTokenAsText, sentenceTokens);
                    
                    var tags = nlpPosTagger.Tag(allSentenceSubTokenAsText.ToArray());
                    var parseTree = nlpParser.DoParse(allSentenceSubTokenAsText.ToArray());
                    tweet.parseTrees.Add(parseTree.GetChildren()[0]);

                    int currentTagIndex = 0;
                    for(int i = 0; i < sentenceTokens.Count; i++)
                    {
                        Token sentenceToken = sentenceTokens[i];
                        for (int j = 0; j < sentenceTokens[i].subTokens.Count; j++)
                        {
                            SubToken subToken = sentenceToken.subTokens[j];
                            if (Enum.TryParse(tags[currentTagIndex], out PosLabels label))
                            {
                                subToken.posLabel = label;
                                sentenceToken.subTokens[j] = subToken;
                            }
                        }
                        sentenceTokens[i] = sentenceToken;
                    }

                    saveSentenceTokensInTweet(ref tweet, sentenceTokens);
                    sentenceTokens.Clear();
                    sentenceIndex++;
                }
            }
        }

        private void saveSentenceTokensInTweet(ref Tweet tweet, List<Token> sentenceTokens)
        {
            foreach (Token sentenceToken in sentenceTokens)
            {
                tweet.allTokens[sentenceToken.indexInTokenList] = sentenceToken;
            }
        }

        private void fillWithAllSubTokensAsText(out List<string> allSentenceSubTokenAsText, List<Token> sentenceTokens)
        {
            allSentenceSubTokenAsText = new List<string>();
            foreach (Token sentenceToken in sentenceTokens)
            {
                allSentenceSubTokenAsText.AddRange(sentenceToken.subTokens.Select(st => st.text));
            }
        }
    }
}
