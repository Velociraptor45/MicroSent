using MicroSent.Models.Constants;
using OpenNLP.Tools.Parser;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models.Analyser
{
    public class PosTagger
    {
        private EnglishTreebankParser nlpParser;

        public PosTagger()
        {
            nlpParser = new EnglishTreebankParser(DataPath.NBIN_FOLDER, true, false);
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
    }
}
