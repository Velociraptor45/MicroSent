using System;

namespace MicroSent
{
    [Serializable]
    public class Word
    {
        public string word { get; set; }
        public int positiveOccurences { get; set; }
        public int negativeOccurences { get; set; }
        public float chiSquareValue { get; set; }

        public Word() { }

        public Word(string word)
        {
            this.word = word;
            this.positiveOccurences = 0;
            this.negativeOccurences = 0;
        }
    }
}
