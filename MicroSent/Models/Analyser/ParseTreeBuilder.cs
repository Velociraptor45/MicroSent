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
    public class ParseTreeBuilder
    {
        #region public methods
        public void buildDependencyTree(Tweet tweet, JArray conllArray, int sentenceIndex)
        {
            List<Node> allNodes = new List<Node>();

            for (int i = 0; i < conllArray.Count; i++)
            {
                Token referencedToken = tweet.sentences[sentenceIndex][i];
                Node node = new Node(referencedToken, null);
                allNodes.Add(node);
            }

            for (int i = 0; i < conllArray.Count; i++)
            {
                JToken conllToken = conllArray[i];
                int parentIndex = conllToken.Value<int>(GoogleParserConstants.TOKEN_HEAD);
                if (parentIndex != -1) // -1 indicates the root node
                {
                    allNodes[i].setParent(allNodes[parentIndex]);
                    allNodes[parentIndex].addChild(allNodes[i]);
                }
            }

            tweet.parseTrees.Add(allNodes.Where(n => n.parent == null).First());
        }
        #endregion
    }
}
