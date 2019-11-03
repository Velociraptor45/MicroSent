using MicroSent.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSent.Models
{
    [Serializable]
    public class Node
    {
        public Node parent { get; private set; }
        public List<Node> children { get; private set; }

        public Token correspondingToken { get; private set; }

        public Node(Token token, Node parent)
        {
            this.parent = parent;
            children = new List<Node>();
            correspondingToken = token;
        }

        public Node(Node parent)
            : this(null, parent) { }

        public Node()
            : this(null, null) { }

        public void addChild(Node child)
        {
            children.Add(child);
        }

        public void setToken(Token token)
        {
            correspondingToken = token;
        }

        public void setParent(Node parent)
        {
            this.parent = parent;
        }
    }
}
