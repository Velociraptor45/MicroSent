using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using Newtonsoft.Json.Linq;
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

        public void cutIntoSentences(Tweet tweet, List<Token> tokens)
        {
            int sentenceIndex = 0;
            int tokenInSentenceIndex = 0;

            foreach(Token token in tokens)
            {
                if (token.isLink || (tokenInSentenceIndex == 0 && token.isPunctuation))
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

        public void buildTreeFromGoogleParser(Tweet tweet, JArray tokens, int sentenceIndex)
        {
            List<Node> allNodes = new List<Node>();
            for(int i = 0; i < tokens.Count; i++)
            {
                removeWronglyParsedTokens(i, tweet, tokens, sentenceIndex);
                
                Node node = new Node(tweet.sentences[sentenceIndex][i], null);
                allNodes.Add(node);
            }

            for(int i = 0; i < tokens.Count; i++)
            {
                JToken token = tokens[i];
                int parentIndex = token.Value<int>(GoogleParserConstants.TOKEN_HEAD);
                if(parentIndex != -1)
                    allNodes[i].setParent(allNodes[parentIndex]);
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
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Corrected sentence: {tweet.getFullSentence(sentenceIndex)}");
                            Console.ResetColor();
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
            for(int i = lastIndexToDelete; i >= firstIndexToDelete; i--)
            {
                tokens.RemoveAt(i);
            }
        }

        public void parseTweet(Tweet tweet)
        {            
            foreach(List<Token> sentenceTokens in tweet.sentences)
            {
                string[] sentenceTokenText = sentenceTokens.Select(t => t.text).ToArray();
                
                var parseTree = nlpParser.DoParse(sentenceTokenText);
                Node rootNode = translateToNodeTree(parseTree.GetChildren()[0], tweet);
                tweet.parseTrees.Add(rootNode);
            }
        }

        private Node translateToNodeTree(Parse parseTree, Tweet tweet)
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
            if(children.Length == 1 && children.First().GetChildren().Length == 0)
            {
                var firstChild = children.First();
                Node node = new Node(tweet.getTokenByIndex(lastTokenId + 1), parentNode);
                if(!Enum.TryParse(partialTree.Type, out PosLabels posLabel))
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
                foreach(var child in children)
                {
                    lastUsedIndex = buildTreePart(child, node, tweet, lastUsedIndex);
                }

                return lastUsedIndex;
            }
        }
    }
}
