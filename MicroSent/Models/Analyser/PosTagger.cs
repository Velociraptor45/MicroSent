using System;
using System.Collections.Generic;
using System.Linq;
using MicroSent.Models.Enums;
using OpenNLP.Tools.PosTagger;

namespace MicroSent.Models.Analyser
{
    public class PosTagger
    {
        private EnglishMaximumEntropyPosTagger nlpPosTagger;
        private string nbinFilePath = @"data\NBIN_files\";

        public PosTagger()
        {
            nlpPosTagger = new EnglishMaximumEntropyPosTagger(nbinFilePath + "EnglishPOS.nbin", nbinFilePath + "tagdict");
        }

        public void tagTweet(ref Tweet tweet)
        {
            List<Token> sentenceTokens = new List<Token>();
            for (int i = 0; i < tweet.allTokens.Count; i++)
            {
                Token currentToken = tweet.allTokens[i];
                sentenceTokens.Add(currentToken);
                if (currentToken.isPunctuation && currentToken.text != ",")
                {
                    var tags = nlpPosTagger.Tag(sentenceTokens.Select(t => t.text).ToArray());
                    for(int j = 0; j < tags.Length; j++)
                    {
                        //translate the tag into PosLabels enum
                        int tokenPosition = sentenceTokens[j].index;
                        if(Enum.TryParse(tags[j], out PosLabels label))
                        {
                            Token token = tweet.allTokens[tokenPosition];
                            token.posLabel = label;
                            tweet.allTokens[tokenPosition] = token;
                        }
                    }
                    sentenceTokens.Clear();
                }
            }
        }
    }
}
