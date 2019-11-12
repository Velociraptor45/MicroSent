using MicroSent.Models.Enums;
using System;

namespace MicroSent.Models
{
    [Serializable]
    public class Smiley
    {
        public string smiley;
        public Polarity polarity;

        public Smiley()
        {

        }

        public Smiley(string smiley, Polarity polarity)
        {
            this.smiley = smiley;
            this.polarity = polarity;
        }
    }
}
