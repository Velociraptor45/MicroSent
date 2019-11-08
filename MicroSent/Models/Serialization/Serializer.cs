using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MicroSent.Models.Serialization
{
    public class Serializer
    {
        public void serializeTweets(List<Tweet> tweets, string filePath)
        {

            using (Stream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, tweets);
            }
        }
    }
}
