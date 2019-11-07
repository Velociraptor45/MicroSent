using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Serialization
{
    public class Item
    {
        [XmlAttribute]
        public string key;
        [XmlAttribute]
        public float value;
    }
}
