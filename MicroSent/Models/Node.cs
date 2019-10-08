using MicroSent.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSent.Models
{
    public class Node
    {
        public PosLabels posLabel { get; private set; }
        public Node parent { get; private set; }
        public List<Node> children { get; private set; }

        public Token correspondingToken { get; private set; }

        public Node(Token token, Node parent, PosLabels posLabel)
        {
            this.posLabel = posLabel;
            this.parent = parent;
            children = new List<Node>();
            correspondingToken = token;
        }

        public void addChild(Node child)
        {
            children.Add(child);
        }
    }
}
