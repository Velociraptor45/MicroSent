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
        public float chiSquareValue { get; set; }

        public List<Tweet> positiveTweets { get; set; }
        public List<Tweet> negativeTweets { get; set; }

        public Word() { }

        public Word(string word)
        {
            this.word = word;
            this.positiveOccurences = 0;
            this.negativeOccurences = 0;

            this.positiveTweets = new List<Tweet>();
            this.negativeTweets = new List<Tweet>();
        }
    }
}
