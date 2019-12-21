using MicroSent.Models;
using System;
using System.Collections.Generic;

namespace LexiconExtension
{
    [Serializable]
    public class Word
    {
        public string word { get; set; }
        public int positiveOccurences { get; set; }
        public int negativeOccurences { get; set; }
        public double chiSquareValue { get; set; }

        public Word() { }

        public Word(string word)
        {
            this.word = word;
            this.positiveOccurences = 0;
            this.negativeOccurences = 0;
        }
    }
}
