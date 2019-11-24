using Serialization.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Serialization
{
    class SlangConverter
    {
        private const string Separator = "---";

        public void convert(string inputPath, string outputPath)
        {
            var slang = loadSlang(inputPath);
            serializeSlang(slang, outputPath);
        }

        private List<Slang> loadSlang(string dataPath)
        {
            List<Slang> slang = new List<Slang>();
            using (StreamReader streamReader = new StreamReader(dataPath))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line != "")
                    {
                        string[] parts = line.Split(Separator);
                        slang.Add(new Slang(parts[0], parts[1]));
                    }
                }
            }
            return slang;
        }

        private void serializeSlang(List<Slang> slang, string outputPath)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<Slang>), new XmlRootAttribute() { ElementName = "slang" });
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                xmlSerializer.Serialize(writer, slang);
            }
        }
    }
}
