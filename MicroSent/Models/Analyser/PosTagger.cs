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
