using System;

namespace Serialization.Models
{
    [Serializable]
    public class Slang
    {
        public string slang { get; set; }
        public string replacement { get; set; }

        public Slang() { }

        public Slang(string slang, string replacement)
        {
            this.slang = slang;
            this.replacement = replacement;
        }
    }
}
