﻿using MicroSent.Models.Constants;
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
        private Regex negationToken = new Regex(@"\bno(t|n-?)?\b|\bnever\b|\bn'?t\b");

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
        public void buildTreeFromGoogleParser(Tweet tweet, JArray tokens, int sentenceIndex)
        {
            List<Node> allNodes = new List<Node>();
            for (int i = 0; i < tokens.Count; i++)
            {
                removeWronglyParsedTokens(i, tweet, tokens, sentenceIndex);

                Node node = new Node(tweet.sentences[sentenceIndex][i], null);
                allNodes.Add(node);
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                JToken token = tokens[i];
                int parentIndex = token.Value<int>(GoogleParserConstants.TOKEN_HEAD);
                if (parentIndex != -1)
                {
                    allNodes[i].setParent(allNodes[parentIndex]);
                    allNodes[parentIndex].addChild(allNodes[i]);
                }
            }

            tweet.parseTrees.Add(allNodes.Where(n => n.parent == null).First());
        }

        private void removeWronglyParsedTokens(int currentTokenIndex, Tweet tweet, JArray tokens, int sentenceIndex)
        {
            JToken token = tokens[currentTokenIndex];
            if (currentTokenIndex < tokens.Count - 1)
            {
                string thisJTokenWord = token.Value<string>(GoogleParserConstants.TOKEN_WORD);
                string thisSentenceTokenWord = tweet.sentences[sentenceIndex][currentTokenIndex].text;
                if (thisJTokenWord != thisSentenceTokenWord && ($"'{thisJTokenWord}" != thisSentenceTokenWord || !(thisJTokenWord == "nt" && thisSentenceTokenWord == "n't")))
                {
                    int deleteUnitilThisIndex = currentTokenIndex + 1;
                    string currentJTokenText = thisJTokenWord;
                    while (deleteUnitilThisIndex < tokens.Count)
                    {
                        string nextJTokenWord = tokens[deleteUnitilThisIndex].Value<string>(GoogleParserConstants.TOKEN_WORD);
                        translateGoogleAbbreviation(ref nextJTokenWord);

                        if (currentJTokenText + nextJTokenWord == thisSentenceTokenWord || $"{currentJTokenText}'{nextJTokenWord}" == thisSentenceTokenWord)
                        {
                            removeTokens(currentTokenIndex + 1, deleteUnitilThisIndex, tokens);
                            ConsolePrinter.printCorrectedGoogleParsing(tweet.getFullSentence(sentenceIndex));
                            break;
                        }
                        currentJTokenText += nextJTokenWord;
                        deleteUnitilThisIndex++;
                    }
                }
            }
        }

        private void translateGoogleAbbreviation(ref string word)
        {
            switch (word)
            {
                case GoogleParserConstants.RIGHT_ROUND_BRACKED:
                    word = ")";
                    break;
                case GoogleParserConstants.LEFT_ROUND_BRACKED:
                    word = "(";
                    break;
            }
        }

        private void removeTokens(int firstIndexToDelete, int lastIndexToDelete, JArray tokens)
        {
            for (int i = lastIndexToDelete; i >= firstIndexToDelete; i--)
            {
                tokens.RemoveAt(i);
            }
        }
        #endregion

        #region stanford parse tree
        public Node translateToNodeTree(Parse parseTree, Tweet tweet)
        {
            Node root = new Node();

            int lastUsedIndex = -1;
            foreach (var child in parseTree.GetChildren())
            {
                lastUsedIndex = buildTreePart(child, root, tweet, lastUsedIndex);
            }

            return root;
        }

        private int buildTreePart(Parse partialTree, Node parentNode, Tweet tweet, int lastTokenId)
        {
            var children = partialTree.GetChildren();
            if (children.Length == 1 && children.First().GetChildren().Length == 0)
            {
                Node node = new Node(tweet.getTokenByIndex(lastTokenId + 1), parentNode);
                if (!Enum.TryParse(partialTree.Type, out PosLabels posLabel))
                {
                    posLabel = PosLabels.Default;
                }
                node.correspondingToken.posLabel = posLabel;
                parentNode.addChild(node);
                return lastTokenId + 1;
            }
            else
            {
                int lastUsedIndex = lastTokenId;
                Node node = new Node(parentNode);
                parentNode.addChild(node);
                foreach (var child in children)
                {
                    lastUsedIndex = buildTreePart(child, node, tweet, lastUsedIndex);
                }

                return lastUsedIndex;
            }
        }

        #endregion
    }
}