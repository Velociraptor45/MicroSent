using MicroSent.Controllers;
using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using MicroSent.Models.Util;
using Newtonsoft.Json.Linq;
using OpenNLP.Tools.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class ParseTreeAnalyser
    {
        private Regex negationToken = new Regex(RegexConstants.NEGATION_TOKEN_PATTERN);

        public void applyGoogleParseTreeNegation(Tweet tweet)
        {
            for(int sentenceIndex = 0; sentenceIndex < tweet.sentences.Count; sentenceIndex++)
            {
                List<Token> tokensToNegate = new List<Token>();
                fillWithTokensToNegate(tweet.parseTrees[sentenceIndex], tokensToNegate);

                foreach(Token token in tokensToNegate)
                {
                    token.negationRating *= RatingConstants.NEGATION;
                }
            }
        }

        private void fillWithTokensToNegate(Node node, List<Token> tokens)
        {
            Match match = negationToken.Match(node.correspondingToken.text);
            if (match.Success)
            {
                if (node.parent != null)
                {
                    tokens.Add(node.parent.correspondingToken);
                    foreach (Node child in node.parent.children)
                    {
                        tokens.Add(child.correspondingToken);
                    }
                    tokens.Remove(node.correspondingToken);
                }
            }

            foreach(Node child in node.children)
            {
                fillWithTokensToNegate(child, tokens);
            }
        }

        #region google parse tree
        public void buildTreeAndTagTokensFromSyntaxNet(Tweet tweet, JArray tokens, int sentenceIndex)
        {
            List<Node> allNodes = new List<Node>();
            List<int> deletedIndexes = new List<int>();
            for (int i = 0; i < tokens.Count; i++)
            {
                List<int> delIndexes = removeWronglyParsedTokens(i, tweet, tokens, sentenceIndex);

                for(int j = 0; j < delIndexes.Count; j++)
                {
                    delIndexes[j] = delIndexes[j] + deletedIndexes.Count;
                }
                deletedIndexes.AddRange(delIndexes);

                string tag = tokens[i].Value<string>(GoogleParserConstants.TOKEN_TAG);
                Token referencedToken = tweet.sentences[sentenceIndex][i];
                setPosLabel(referencedToken, tag);

                Node node = new Node(referencedToken, null);
                allNodes.Add(node);
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                JToken token = tokens[i];
                int originalParentIndex = token.Value<int>(GoogleParserConstants.TOKEN_HEAD);
                //the parent index must be adapted because previous indexes might have been deleted
                int parentIndex = originalParentIndex - deletedIndexes.Count(ind => ind <= originalParentIndex);
                if (parentIndex != -1)
                {
                    allNodes[i].setParent(allNodes[parentIndex]);
                    allNodes[parentIndex].addChild(allNodes[i]);
                }
            }

            tweet.parseTrees.Add(allNodes.Where(n => n.parent == null).First());
        }

        private List<int> removeWronglyParsedTokens(int currentTokenIndex, Tweet tweet, JArray tokens, int sentenceIndex)
        {
            List<int> deletedIndexes = new List<int>();
            JToken token = tokens[currentTokenIndex];
            if (currentTokenIndex < tokens.Count - 1)
            {
                string thisJTokenWord = token.Value<string>(GoogleParserConstants.TOKEN_WORD);
                string thisSentenceTokenWord = UnicodeHelper.removeNonUnicodeCharacters(tweet.sentences[sentenceIndex][currentTokenIndex].text);
                if (thisJTokenWord != thisSentenceTokenWord
                    && ($"'{thisJTokenWord}" != thisSentenceTokenWord
                        || !(thisJTokenWord == TokenPartConstants.NEGATION_TOKEN_ENDING_WITHOUT_APOSTROPHE 
                            && thisSentenceTokenWord == TokenPartConstants.NEGATION_TOKEN_ENDING_WITH_APOSTROPHE)))
                {
                    int deleteUnitilThisIndex = currentTokenIndex + 1;
                    string currentJTokenText = thisJTokenWord;
                    while (deleteUnitilThisIndex < tokens.Count)
                    {
                        string nextJTokenWord = tokens[deleteUnitilThisIndex].Value<string>(GoogleParserConstants.TOKEN_WORD);
                        translateGoogleAbbreviation(ref nextJTokenWord);

                        if (currentJTokenText + nextJTokenWord == thisSentenceTokenWord
                            || $"{currentJTokenText}{TokenPartConstants.APOSTROPHE}{nextJTokenWord}" == thisSentenceTokenWord)
                        {
                            deletedIndexes = removeTokens(currentTokenIndex + 1, deleteUnitilThisIndex, tokens);
                            ConsolePrinter.printCorrectedGoogleParsing(tweet.getFullUnicodeSentence(sentenceIndex));
                            break;
                        }
                        currentJTokenText += nextJTokenWord;
                        deleteUnitilThisIndex++;
                    }
                }
            }
            return deletedIndexes;
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

        private void setPosLabel(Token token, string tag)
        {
            PosLabels posLabel = Converter.convertTagToPosLabel(tag);
            token.posLabel = posLabel;
        }
        #endregion
    }
}
