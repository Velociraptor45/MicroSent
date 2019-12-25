using MicroSent.Models.Configuration;
using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using MicroSent.Models.Network;
using MicroSent.Models.Util;
using Newtonsoft.Json.Linq;
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

        #region constructors
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

        public JArray correctSyntaxNetTokenizingDifferences(Tweet tweet, JArray conllArray, int sentenceIndex)
        {
            JArray correctedConllTokens = new JArray();
            List<int> allRemovedIndexes = new List<int>();
            for (int i = 0; i < conllArray.Count; i++)
            {
                List<int> newlyRemovedIndexes = removeWronglyParsedTokensIfNecessary(i, tweet, conllArray, sentenceIndex);
                correctRemovedIndexes(newlyRemovedIndexes, allRemovedIndexes.Count);
                allRemovedIndexes.AddRange(newlyRemovedIndexes);

                addNewJObjectToArray(conllArray[i], correctedConllTokens);
            }

            updateJTokenHeads(correctedConllTokens, allRemovedIndexes);

            return correctedConllTokens;
        }
        #endregion

        #region private methods
        private void correctRemovedIndexes(List<int> removedIndexes, int amountOfPreviouslyRemovedIndexes)
        {
            // The removed conllArray indexes might not be correct because
            // there might already be tokens removed before. The currently removed indexes
            // must be corrected to match their original index

            if (amountOfPreviouslyRemovedIndexes == 0)
                return;

            for (int j = 0; j < removedIndexes.Count; j++)
            {
                removedIndexes[j] = removedIndexes[j] + amountOfPreviouslyRemovedIndexes;
            }
        }

        private List<int> removeWronglyParsedTokensIfNecessary(int currentTokenIndex, Tweet tweet,
            JArray conllArray, int sentenceIndex)
        {
            JToken token = conllArray[currentTokenIndex];

            // the array count is reduced by 1 because you don't need to check the last
            // token. There won't be any other tokens after it that you can concatinate
            // it with if the jToken und the sentence token don't match
            if (currentTokenIndex < conllArray.Count - 1)
            {
                string jTokenText = token.Value<string>(GoogleParserConstants.TOKEN_WORD);
                string sentenceTokenText = UnicodeHelper.removeNonUnicodeCharacters(tweet.sentences[sentenceIndex][currentTokenIndex].text);
                if (!areTokensEqual(jTokenText, sentenceTokenText))
                {
                    var indexRangeToRemove = getTokenIndexRangeToRemove(currentTokenIndex, jTokenText,
                        sentenceTokenText, conllArray);
                    if (indexRangeToRemove != null)
                    {
                        ConsolePrinter.printCorrectedGoogleParsing(tweet.getFullUnicodeSentence(sentenceIndex));
                        return removeTokens(indexRangeToRemove.Item1, indexRangeToRemove.Item2, conllArray);
                    }
                }
            }
            return new List<int>();
        }

        private Tuple<int, int> getTokenIndexRangeToRemove(
            int currentTokenIndex, string startJTokenText, string tokenTextToMatch, JArray conllArray)
        {
            int deleteUnitilThisIndex = currentTokenIndex + 1;
            string concatinatedJTokenText = startJTokenText;
            while (deleteUnitilThisIndex < conllArray.Count)
            {
                string nextJTokenText = conllArray[deleteUnitilThisIndex].Value<string>(GoogleParserConstants.TOKEN_WORD);
                translateGoogleAbbreviation(ref nextJTokenText);

                if (areTokensEqual(concatinatedJTokenText + nextJTokenText, tokenTextToMatch))
                {
                    // the token at the current index mustn't be deleted because
                    // it represents the jToken that has to be matched to the sentence token (from the tweet)
                    // it doesn't matter that the content isn't the same - only the indexes are imporant
                    return new Tuple<int, int>(currentTokenIndex + 1, deleteUnitilThisIndex);
                }
                concatinatedJTokenText += nextJTokenText;
                deleteUnitilThisIndex++;
            }
            // Adding the text of jTokens couldn't match the tokenTextToMatch string.
            // It's hard to handle this because this means that the application tokenizer
            // and the SyntaxNet tokenizer are working totally different in this case (or
            // the communication via network is making problem.
            // But normally this shouldn't happen - and if it does the code must be adapted
            // to cover this case
            return null;
        }

        private bool areTokensEqual(string jTokenText, string sentenceTokenText)
        {
            // The apostrophe at the start of jTokenText is checked because
            // that's (apart from the negation ending n't) the only position where
            // apostrophes can occure and the network communication erases them during transmission.
            // This might have to be fixed in the future - but it works currently
            return jTokenText == sentenceTokenText
                    || ($"{TokenPartConstants.APOSTROPHE}{jTokenText}" == sentenceTokenText)
                    || (isNegationEnding(jTokenText) && isNegationEnding(sentenceTokenText));
        }

        private bool isNegationEnding(string tokenText)
        {
            return tokenText == TokenPartConstants.NEGATION_TOKEN_ENDING_WITH_APOSTROPHE
                            || tokenText == TokenPartConstants.NEGATION_TOKEN_ENDING_WITHOUT_APOSTROPHE;
        }

        private void updateJTokenHeads(JArray correctedConllTokens, List<int> allDeletedIndexes)
        {
            foreach (JObject correctedToken in correctedConllTokens)
            {
                int originalParentIndex = correctedToken.Value<int>(GoogleParserConstants.TOKEN_HEAD);
                if (originalParentIndex != -1)
                {
                    int newParentIndex = originalParentIndex - allDeletedIndexes.Count(ind => ind <= originalParentIndex);
                    if (newParentIndex != originalParentIndex)
                    {
                        correctedToken.Remove(GoogleParserConstants.TOKEN_HEAD);
                        correctedToken.Add(GoogleParserConstants.TOKEN_HEAD, newParentIndex);
                    }
                }
            }
        }

        private void addNewJObjectToArray(JToken conllToken, JArray correctedConllTokens)
        {
            JObject correctedToken = new JObject();

            string tag = conllToken.Value<string>(GoogleParserConstants.TOKEN_TAG);
            string word = conllToken.Value<string>(GoogleParserConstants.TOKEN_WORD);
            int head = conllToken.Value<int>(GoogleParserConstants.TOKEN_HEAD);

            correctedToken.Add(GoogleParserConstants.TOKEN_TAG, tag);
            correctedToken.Add(GoogleParserConstants.TOKEN_WORD, word);
            correctedToken.Add(GoogleParserConstants.TOKEN_HEAD, head);
            correctedConllTokens.Add(correctedToken);
        }

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
                JArray correctedConllArray = correctSyntaxNetTokenizingDifferences(tweet, conllArray, i);
                mapPosLabelsOfConllArrayToTokens(correctedConllArray, tweet.sentences[i]);
                parseTreeAnalyser.buildDependencyTree(tweet, correctedConllArray, sentenceIndex: i);
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

        private void translateGoogleAbbreviation(ref string word)
        {
            switch (word)
            {
                case GoogleParserConstants.RIGHT_ROUND_BRACKED:
                    word = TokenPartConstants.CLOSING_BRACKET;
                    break;
                case GoogleParserConstants.LEFT_ROUND_BRACKED:
                    word = TokenPartConstants.OPENING_BRACKET;
                    break;
            }
        }

        private List<int> removeTokens(int firstIndexToDelete, int lastIndexToDelete, JArray tokens)
        {
            List<int> deletedIndexes = new List<int>();
            for (int i = lastIndexToDelete; i >= firstIndexToDelete; i--)
            {
                tokens.RemoveAt(i);
                deletedIndexes.Add(i);
            }
            return deletedIndexes;
        }
        #endregion
    }
}
