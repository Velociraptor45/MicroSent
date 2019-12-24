using MicroSent.Models.Configuration;
using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using MicroSent.Models.Network;
using MicroSent.Models.Util;
using Newtonsoft.Json.Linq;
using OpenNLP.Tools.Parser;
using OpenNLP.Tools.PosTagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSent.Models.Analyser
{
    public class PosTagger
    {
        #region private members
        private EnglishMaximumEntropyPosTagger nlpPosTagger;

        private NetworkClientSocket networkSendClientSocket;
        private NetworkClientSocket networkReceiveClientSocket;

        private ParseTreeAnalyser parseTreeAnalyser;

        private IAlgorithmConfiguration configuration;
        #endregion

        #region constructor
        public PosTagger(IAlgorithmConfiguration configuration)
        {
            this.configuration = configuration;

            nlpPosTagger = new EnglishMaximumEntropyPosTagger(
                DataPath.NBIN_FOLDER + "EnglishPOS.nbin", DataPath.NBIN_FOLDER + "tagdict");

            networkSendClientSocket = new NetworkClientSocket(
                configuration.clientSendingPort, configuration.clientHost);
            networkReceiveClientSocket = new NetworkClientSocket(
                configuration.clientReceivingPort, configuration.clientHost);

            parseTreeAnalyser = new ParseTreeAnalyser();
        }
        #endregion

        #region public methods
        public void cutTokensIntoSentences(Tweet tweet, List<Token> tokens)
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


                if (token.isPunctuation)
                {
                    tokenInSentenceIndex = 0;
                    sentenceIndex++;
                    continue;
                }
                tokenInSentenceIndex++;
            }
        }

        public async Task tagAllTokensOfTweet(Tweet tweet)
        {
            if (configuration.useGoogleParser)
            {
                await tagSentencesWithSyntaxNet(tweet);
                await tagHashtagsWithSyntaxNet(tweet);
            }
            else
            {
                tagAllTokensWithStanford(tweet);
            }
        }
        #endregion

        #region private methods
        private async Task<JArray> getConllArrayFromServer(string sentence)
        {
            networkSendClientSocket.sendStringToServer(sentence);
            string serverAnswere = await networkReceiveClientSocket.receiveParseTree();

            JObject conllJSON = JObject.Parse(serverAnswere);

            return conllJSON.Value<JArray>(GoogleParserConstants.TOKEN_ARRAY);
        }

        private async Task tagSentencesWithSyntaxNet(Tweet tweet)
        {
            for (int i = 0; i < tweet.sentences.Count; i++)
            {
                JArray conllArray = await getConllArrayFromServer(tweet.getFullUnicodeSentence(i));
                JArray correctedConllArray = parseTreeAnalyser.correctSyntaxNetTokenizingDifferences(tweet, conllArray, i);
                mapPosLabelsOfConllArrayToTokens(correctedConllArray, tweet.sentences[i]);
                parseTreeAnalyser.buildDependencyTree(tweet, correctedConllArray, sentenceIndex: i);
                //parseTreeAnalyser.buildTreeAndTagTokensFromSyntaxNet(tweet, conllArray, i);
            }
        }

        private async Task tagHashtagsWithSyntaxNet(Tweet tweet)
        {
            for (int i = 0; i < tweet.rest.Count; i++)
            {
                if (tweet.rest[i].isHashtag)
                {
                    Token hashtagToken = tweet.rest[i];
                    JArray conllArray = await getConllArrayFromServer(tweet.getFullUnicodeRestToken(i));

                    mapPosLabelsOfConllArrayToTokens(conllArray, new List<Token>() { hashtagToken },
                        isHashtagAnalysis: true);
                }
            }
        }


        private void tagAllTokensWithStanford(Tweet tweet)
        {
            foreach (List<Token> sentenceTokens in tweet.sentences)
            {
                var tags = tagSequence(sentenceTokens);
                mapTagsToTokens(sentenceTokens, tags);
            }
            foreach (Token token in tweet.rest.Where(t => t.isHashtag))
            {
                if (token.subTokens.Count > 0)
                {
                    var tags = tagSequence(token.subTokens);
                    mapTagsToSubTokens(token.subTokens, tags);
                }
                else
                {
                    string[] tags = tagSequence(new string[] { token.text });
                    mapTagsToTokens(new List<Token>() { token }, tags);
                }
            }
        }

        private void mapPosLabelsOfConllArrayToTokens(JArray conllArray, List<Token> tokens,
            bool isHashtagAnalysis = false)
        {
            for (int i = 0; i < conllArray.Count; i++)
            {
                JToken conllToken = conllArray[i];
                string tag = conllToken.Value<string>(GoogleParserConstants.TOKEN_TAG);
                PosLabels posLabel = Converter.convertTagToPosLabel(tag);

                if (isHashtagAnalysis)
                {
                    // if this mapping is part of a Hashtag analysis, the analysed words
                    // are SubTokens of a Hashtag Token -> The tokens list contains only
                    // the Hashtag token
                    tokens.First().subTokens[i].posLabel = posLabel;
                }
                else
                {
                    tokens[i].posLabel = posLabel;
                }
            }
        }

        private void mapTagsToTokens(List<Token> tokenSequence, string[] tags)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                tokenSequence[i].posLabel = Converter.convertTagToPosLabel(tags[i]);
            }
        }

        private void mapTagsToSubTokens(List<SubToken> subTokenSequence, string[] tags)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                subTokenSequence[i].posLabel = Converter.convertTagToPosLabel(tags[i]);
            }
        }

        private string[] tagSequence(string[] tokens)
        {
            return nlpPosTagger.Tag(tokens);
        }

        private string[] tagSequence(List<Token> tokensList)
        {
            return tagSequence(tokensList.Select(t => t.text).ToArray());
        }

        private string[] tagSequence(List<SubToken> subTokensList)
        {
            return tagSequence(subTokensList.Select(t => t.text).ToArray());
        }
        #endregion
    }
}
