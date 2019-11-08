using MicroSent.Models.Util;
using MicroSent.Models.Serialization;
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

        Deserializer deserializer = new Deserializer(RootName, FilePath + DataFileName, typeof(Item[]));
        private static Dictionary<string, float> testTweetsDictionary;

        public Tester()
        {
            if(testTweetsDictionary == null)
            {
                deserializer.deserializeDictionary(out testTweetsDictionary);
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
            AnalyisContainer analyisContainer = new AnalyisContainer();

            foreach (Tweet tweet in tweets)
            {
                float rating = 0f;
                string expectedRating = getExpectedRating(tweet.testRating);
                string actualRating = getActualRating(tweet.positiveRating, tweet.negativeRating, ref rating);

                analyseResult(expectedRating, actualRating, ref analyisContainer);

                printAnalysisInfo(tweet, expectedRating, actualRating, rating);
            }
            int correctTotal = analyisContainer.correctPositive + analyisContainer.correctNegative + analyisContainer.correctNeutral;
            float correctPercentage = ((float)correctTotal) / tweets.Count * 100;

            int totalPositive = tweets.Count(t => t.testRating == 4f);
            float totalPositivePercentage = ((float)analyisContainer.correctPositive) / totalPositive * 100;

            int totalNegative = tweets.Count(t => t.testRating == 0f);
            float totalNegativePercentage = ((float)analyisContainer.correctNegative) / totalNegative * 100;

            int totalNeutral = tweets.Count(t => t.testRating == 2f);
            float totalNeutralPercentage = ((float)analyisContainer.correctNeutral) / totalNeutral * 100;

            Console.WriteLine("######################################################################");
            Console.WriteLine($"{correctTotal} of {tweets.Count} ({correctPercentage}%) correct.");
            Console.WriteLine($"Positive: {analyisContainer.correctPositive} of {totalPositive} ({totalPositivePercentage}%)");
            Console.WriteLine($"Negative: {analyisContainer.correctNegative} of {totalNegative} ({totalNegativePercentage}%)");
            Console.WriteLine($"Neutral: {analyisContainer.correctNeutral} of {totalNeutral} ({totalNeutralPercentage}%)");
            Console.WriteLine($"False positives: {analyisContainer.falsePositive}");
            Console.WriteLine($"\t- should be negative: {analyisContainer.shouldNegativeButIsPositive}");
            Console.WriteLine($"\t- should be neutral: {analyisContainer.shouldNeutralButIsPositive}");
            Console.WriteLine($"False negatives: {analyisContainer.falseNegative}");
            Console.WriteLine($"\t- should be positive: {analyisContainer.shouldPositiveButIsNegative}");
            Console.WriteLine($"\t- should be neutral: {analyisContainer.shouldNeutralButIsNegative}");
            Console.WriteLine($"False neutrals: {analyisContainer.falseNeutral}");
            Console.WriteLine($"\t- should be positive: {analyisContainer.shouldPositiveButIsNeutral}");
            Console.WriteLine($"\t- should be negative: {analyisContainer.shouldNegativeButIsNeutral}");
            Console.WriteLine($"Indecisive: {analyisContainer.indecisive}");
            Console.WriteLine($"\t- should be positive: {analyisContainer.shouldPositiveButIsEqual}");
            Console.WriteLine($"\t- should be negative: {analyisContainer.shouldNegativeButIsEqual}");
            Console.WriteLine($"\t- should be neutral: {analyisContainer.shouldNeutralButIsEqual}");
            Console.WriteLine("######################################################################");
        }

        private void printAnalysisInfo(Tweet tweet, string expectedRating, string actualRating, float rating)
        {
            Console.WriteLine("________________________________________________________________");
            Console.WriteLine(tweet.fullText);

            if(expectedRating != actualRating)
                Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine($"Expected {expectedRating} and got {actualRating} ({rating})");

            Console.ResetColor();


            Console.WriteLine($"Positive Rating: {tweet.positiveRating}");
            foreach (Token token in tweet.sentences.SelectMany(s => s).Where(t => t.totalRating > 0))
            {
                Console.Write(token.text + $"({token.totalRating}), ");
            }
            foreach(Token token in tweet.rest.Where(t => t.totalRating > 0))
            {
                Console.Write(token.text + $"({token.totalRating}), ");
            }
            Console.WriteLine("");
            Console.WriteLine($"Negative Rating: {tweet.negativeRating}");
            foreach (Token token in tweet.sentences.SelectMany(s => s).Where(t => t.totalRating < 0))
            {
                Console.Write(token.text + $"({token.totalRating}), ");
            }
            foreach (Token token in tweet.rest.Where(t => t.totalRating < 0))
            {
                Console.Write(token.text + $"({token.totalRating}), ");
            }
            Console.WriteLine("");
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

        private void analyseResult(string expectedRating, string actualRating, ref AnalyisContainer analyisContainer)
        {
            if (expectedRating == actualRating)
            {
                if (actualRating == Positive)
                    analyisContainer.correctPositive++;
                else if (actualRating == Negative)
                    analyisContainer.correctNegative++;
                else
                    analyisContainer.correctNeutral++;
            }
            else if (actualRating == Positive)
            {
                analyisContainer.falsePositive++;
                if (expectedRating == Negative)
                    analyisContainer.shouldNegativeButIsPositive++;
                else if (expectedRating == Neutral)
                    analyisContainer.shouldNeutralButIsPositive++;
            }
            else if (actualRating == Negative)
            {
                analyisContainer.falseNegative++;
                if (expectedRating == Positive)
                    analyisContainer.shouldPositiveButIsNegative++;
                else if (expectedRating == Positive)
                    analyisContainer.shouldPositiveButIsNegative++;
            }
            else if (actualRating == Neutral)
            {
                analyisContainer.falseNeutral++;
                if (expectedRating == Positive)
                    analyisContainer.shouldPositiveButIsNeutral++;
                else if (expectedRating == Negative)
                    analyisContainer.shouldNegativeButIsNeutral++;
            }
            else if (actualRating == Equal)
            {
                analyisContainer.indecisive++;
                if (expectedRating == Positive)
                    analyisContainer.shouldPositiveButIsEqual++;
                else if (expectedRating == Negative)
                    analyisContainer.shouldNegativeButIsEqual++;
                else
                    analyisContainer.shouldNeutralButIsEqual++;
            }
        }
    }

    public struct AnalyisContainer
    {
        public int correctPositive;
        public int correctNegative;
        public int correctNeutral;
        public int falsePositive;
        public int falseNegative;
        public int falseNeutral;
        public int indecisive;

        public int shouldNeutralButIsPositive;
        public int shouldNegativeButIsPositive;

        public int shouldNeutralButIsNegative;
        public int shouldPositiveButIsNegative;

        public int shouldPositiveButIsNeutral;
        public int shouldNegativeButIsNeutral;

        public int shouldNeutralButIsEqual;
        public int shouldNegativeButIsEqual;
        public int shouldPositiveButIsEqual;

        public AnalyisContainer(int a)
        {
            correctPositive = 0;
            correctNegative = 0;
            correctNeutral = 0;
            falsePositive = 0;
            falseNegative = 0;
            falseNeutral = 0;
            indecisive = 0;

            shouldNeutralButIsPositive = 0;
            shouldNegativeButIsPositive = 0;

            shouldNeutralButIsNegative = 0;
            shouldPositiveButIsNegative = 0;

            shouldPositiveButIsNeutral = 0;
            shouldNegativeButIsNeutral = 0;

            shouldNeutralButIsEqual = 0;
            shouldNegativeButIsEqual = 0;
            shouldPositiveButIsEqual = 0;
        }
    }
}