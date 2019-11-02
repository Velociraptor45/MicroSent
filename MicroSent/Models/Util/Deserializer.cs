using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace MicroSent.Models.Util
{
    public class Deserializer
    {
        XmlSerializer xmlSerializer;
        private string filePath;

        public Deserializer(string rootElement, string filePath)
        {
            xmlSerializer = new XmlSerializer(typeof(Item[]), new XmlRootAttribute() { ElementName = rootElement });
            this.filePath = filePath;
        }

        public Deserializer()
        {

        }

        public void loadDictionary(out Dictionary<string, float> dictionary)
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                dictionary = ((Item[])xmlSerializer.Deserialize(streamReader)).ToDictionary(e => e.key, e => e.value);
            }
        }



        public List<Tweet> deserializeTweets(string filePath)
        {
            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                IFormatter formatter = new BinaryFormatter();
                return (List<Tweet>)formatter.Deserialize(stream);
            }
        }
    }
}
