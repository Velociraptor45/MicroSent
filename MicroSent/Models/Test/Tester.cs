using MicroSent.Models.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models.Test
{
    public class Tester
    {
        private const string RootName = "TestData";
        private const string FilePath = @"data\testdata\";
        private const string DataFileName = "testdata.xml";

        private const string Positive = "positive";
        private const string Negative = "negative";
        private const string Neutral = "neutral";
        private const string Equal = "equal";

        Deserializer deserializer = new Deserializer(RootName, FilePath + DataFileName);
        private static Dictionary<string, float> testTweetsDictionary;

        public Tester()
        {
            if(testTweetsDictionary == null)
            {
                deserializer.loadDictionary(out testTweetsDictionary);
            }
        }

        public List<Tweet> getTestTweets()
        {
            List<Tweet> tweets = new List<Tweet>();

            List<string> dictionaryKeys = testTweetsDictionary.Keys.ToList();
            foreach(string key in dictionaryKeys)
            {
                Tweet tweet = new Tweet(key, "", 0);
                tweet.testRating = testTweetsDictionary[key];
                tweets.Add(tweet);
            }

            return tweets;
        }

        public void checkTweetRating(List<Tweet> tweets)
        {
            int correct = 0;
            int falsePositive = 0;
            int falseNegative = 0;
            int falseNeutral = 0;
            int indecisive = 0;

            foreach (Tweet tweet in tweets)
            {
                float rating = 0f;
                string expectedRating = getExpectedRating(tweet.testRating);
                string actualRating = getActualRating(tweet.positiveRating, tweet.negativeRating, ref rating);

                analyseResult(expectedRating, actualRating, ref correct, ref falsePositive, ref falseNegative,
                    ref falseNeutral, ref indecisive);
                                
                Console.WriteLine("________________________________________________________________");
                Console.WriteLine($"{tweet}");
                Console.WriteLine($"Expected {expectedRating} and got {actualRating} ({rating})");
            }
            Console.WriteLine("######################################################################");
            Console.WriteLine($"{correct} of {tweets.Count} ({((float)correct) / ((float)tweets.Count)}%) correct.");
            Console.WriteLine($"False positives: {falsePositive}");
            Console.WriteLine($"False negatives: {falseNegative}");
            Console.WriteLine($"False neutrals: {falseNeutral}");
            Console.WriteLine($"Indecisive: {indecisive}");
            Console.WriteLine("######################################################################");
        }

        private string getExpectedRating(float testRating)
        {
            switch (testRating)
            {
                case 0f:
                    return Negative;
                case 2f:
                    return Neutral;
                case 4f:
                    return Positive;
                default:
                    return "";
            }
        }

        private string getActualRating(float positiveRating, float negativeRating, ref float rating)
        {
            if (negativeRating == 0f && positiveRating == 0f)
            {
                return Neutral;
            }
            else
            {
                if (Math.Abs(negativeRating) > positiveRating)
                {
                    rating = negativeRating;
                    return Negative;
                }
                else if (Math.Abs(negativeRating) < positiveRating)
                {
                    rating = positiveRating;
                    return Positive;
                }
                else
                {
                    rating = positiveRating;
                    return Equal;
                }
            }
        }

        private void analyseResult(string expectedRating, string actualRating,
            ref int correct, ref int falsePositive, ref int falseNegative, ref int falseNeutral, ref int indecisive)
        {
            if (expectedRating == actualRating)
            {
                correct++;
            }
            else if (actualRating == Positive)
            {
                falsePositive++;
            }
            else if (actualRating == Negative)
            {
                falseNegative++;
            }
            else if (actualRating == Neutral)
            {
                falseNeutral++;
            }
            else if (actualRating == Equal)
            {
                indecisive++;
            }
        }
    }
}