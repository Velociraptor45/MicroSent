using System.Xml;
using System.Xml.Serialization;

namespace MicroSent.Models
{
    public class Item
    {
        [XmlAttribute]
        public string key;
        [XmlAttribute]
        public float value;
    }
}
