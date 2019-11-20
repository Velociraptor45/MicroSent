using MicroSent.Models.Constants;
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
        private EnglishTreebankParser nlpParser;
        private EnglishMaximumEntropyPosTagger nlpPosTagger;

        public PosTagger()
        {
            nlpParser = new EnglishTreebankParser(DataPath.NBIN_FOLDER, true, false);
            nlpPosTagger = new EnglishMaximumEntropyPosTagger(DataPath.NBIN_FOLDER + "EnglishPOS.nbin", DataPath.NBIN_FOLDER + "tagdict");
        }

        public void cutIntoSentences(Tweet tweet, List<Token> tokens)
        {
            int sentenceIndex = 0;
            int tokenInSentenceIndex = 0;

            foreach(Token token in tokens)
            {
                if (token.isLink || token.isEmoji || token.isSmiley || (tokenInSentenceIndex == 0 && token.isPunctuation))
                {
                    tweet.rest.Add(token);
                    continue;
                }

                if(tweet.firstEndHashtagIndex != -1 && token.indexInTweet >= tweet.firstEndHashtagIndex)
                {
                    tweet.rest.Add(token);
                    continue;
                }

                if (tokenInSentenceIndex == 0)
                    tweet.sentences.Add(new List<Token>());

                token.indexInSentence = tokenInSentenceIndex;
                tweet.sentences[sentenceIndex].Add(token);


                if (token.isPunctuation && token.text != ",")
                {
                    tokenInSentenceIndex = 0;
                    sentenceIndex++;
                    continue;
                }
                tokenInSentenceIndex++;
            }
        }

        public Parse parseTweet(List<Token> sentence)
        {
            string[] sentenceTokenText = sentence.Select(t => t.text).ToArray();
            return nlpParser.DoParse(sentenceTokenText).GetChildren()[0];
        }

        public void tagAllTokens(Tweet tweet)
        {
            foreach (List<Token> sentenceTokens in tweet.sentences)
            {
                var tags = nlpPosTagger.Tag(sentenceTokens.Select(t => t.text).ToArray());
                for (int j = 0; j < tags.Length; j++)
                {
                    if (!Enum.TryParse(tags[j], out PosLabels label))
                    {
                        label = PosLabels.Default;
                    }
                    sentenceTokens[j].posLabel = label;
                }
            }
        }
    }
}
