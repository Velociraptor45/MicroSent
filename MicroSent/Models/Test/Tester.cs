using MicroSent.Models.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using MicroSent.Models.Constants;
using MicroSent.Models.Enums;

namespace MicroSent.Models.Test
{
    public class Tester
    {
        private const string RootName = "TestData";

        Deserializer deserializer = new Deserializer(RootName, DataPath.TEST_DATA, typeof(Item[]));
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
                tweet.annotatedPolarity = getPolarityFromRating(testTweetsDictionary[key]);
                tweets.Add(tweet);
            }

            return tweets;
        }

        public void checkTweetRating(List<Tweet> tweets)
        {
            TestRatingAnalyis testRatingAnalyis = new TestRatingAnalyis();

            foreach (Tweet tweet in tweets)
            {
                float rating = 0f;
                Polarity actualPolarity = getActualRating(tweet.positiveRating, tweet.negativeRating, ref rating);

                analyseResult(tweet.annotatedPolarity, actualPolarity, ref testRatingAnalyis);

                printAnalysisInfo(tweet, tweet.annotatedPolarity, actualPolarity, rating);
            }

            calculatePrecisionValues(ref testRatingAnalyis);
            calculateRecallValues(tweets, ref testRatingAnalyis);
            calculateF1Score(ref testRatingAnalyis);

            int correctTotal = testRatingAnalyis.correctPositive + testRatingAnalyis.correctNegative + testRatingAnalyis.correctNeutral;
            float correctPercentage = getPercentage(correctTotal, tweets.Count);

            int totalPositive = getCountWithPolarity(tweets, Polarity.Positive);
            int totalNegative = getCountWithPolarity(tweets, Polarity.Negative);
            int totalNeutral = getCountWithPolarity(tweets, Polarity.Neutral);

            Console.WriteLine("######################################################################");
            Console.WriteLine($"{correctTotal} of {tweets.Count} ({correctPercentage}%) correct.");
            Console.WriteLine($"Positive: {testRatingAnalyis.correctPositive} of {totalPositive}");
            Console.WriteLine($"Negative: {testRatingAnalyis.correctNegative} of {totalNegative}");
            Console.WriteLine($"Neutral: {testRatingAnalyis.correctNeutral} of {totalNeutral}");
            Console.WriteLine("--------------------------------------");
            Console.WriteLine($"Recall (positive): {testRatingAnalyis.recallPositive * 100}%");
            Console.WriteLine($"Recall (negative): {testRatingAnalyis.recallNegative * 100}%");
            Console.WriteLine($"Recall (neutral): {testRatingAnalyis.recallNeutral * 100}%");
            Console.WriteLine("----------");
            Console.WriteLine($"Precision (positive): {testRatingAnalyis.precisionPositive * 100}%");
            Console.WriteLine($"Precision (negative): {testRatingAnalyis.precisionNegative * 100}%");
            Console.WriteLine($"Precision (neutral): {testRatingAnalyis.precisionNeutral * 100}%");
            Console.WriteLine("----------");
            Console.WriteLine($"F1-score (positive): {testRatingAnalyis.f1ScorePositive * 100}%");
            Console.WriteLine($"F1-score (negative): {testRatingAnalyis.f1ScoreNegative * 100}%");
            Console.WriteLine($"F1-score (neutral): {testRatingAnalyis.f1ScoreNeutral * 100}%");
            Console.WriteLine("--------------------------------------");
            Console.WriteLine($"False positives: {testRatingAnalyis.falsePositive}");
            Console.WriteLine($"\t- should be negative: {testRatingAnalyis.shouldNegativeButIsPositive}");
            Console.WriteLine($"\t- should be neutral: {testRatingAnalyis.shouldNeutralButIsPositive}");
            Console.WriteLine($"False negatives: {testRatingAnalyis.falseNegative}");
            Console.WriteLine($"\t- should be positive: {testRatingAnalyis.shouldPositiveButIsNegative}");
            Console.WriteLine($"\t- should be neutral: {testRatingAnalyis.shouldNeutralButIsNegative}");
            Console.WriteLine($"False neutrals: {testRatingAnalyis.falseNeutral}");
            Console.WriteLine($"\t- should be positive: {testRatingAnalyis.shouldPositiveButIsNeutral}");
            Console.WriteLine($"\t- should be negative: {testRatingAnalyis.shouldNegativeButIsNeutral}");
            Console.WriteLine($"Indecisive: {testRatingAnalyis.indecisive}");
            Console.WriteLine($"\t- should be positive: {testRatingAnalyis.shouldPositiveButIsEqual}");
            Console.WriteLine($"\t- should be negative: {testRatingAnalyis.shouldNegativeButIsEqual}");
            Console.WriteLine($"\t- should be neutral: {testRatingAnalyis.shouldNeutralButIsEqual}");
            Console.WriteLine("######################################################################");
        }

        private void printAnalysisInfo(Tweet tweet, Polarity expectedPolarity, Polarity actualPolarity, float rating)
        {
            Console.WriteLine("________________________________________________________________");
            Console.WriteLine(tweet.fullText);

            if(expectedPolarity != actualPolarity)
                Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine($"Expected {expectedPolarity} and got {actualPolarity} ({rating})");

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

        private Polarity getPolarityFromRating(float rating)
        {
            switch (rating)
            {
                case 0f:
                    return Polarity.Negative;
                case 2f:
                    return Polarity.Neutral;
                case 4f:
                    return Polarity.Positive;
                default:
                    throw new KeyNotFoundException($"Can't recognize rating key {rating}");
            }
        }

        private Polarity getActualRating(float positiveRating, float negativeRating, ref float rating)
        {
            if (negativeRating == 0f && positiveRating == 0f)
            {
                return Polarity.Neutral;
            }
            else
            {
                if (Math.Abs(negativeRating) > positiveRating)
                {
                    rating = negativeRating;
                    return Polarity.Negative;
                }
                else if (Math.Abs(negativeRating) < positiveRating)
                {
                    rating = positiveRating;
                    return Polarity.Positive;
                }
                else
                {
                    rating = positiveRating;
                    return Polarity.Negative;
                }
            }
        }

        private void analyseResult(Polarity expectedRating, Polarity actualRating, ref TestRatingAnalyis testRatingAnalyis)
        {
            if (expectedRating == actualRating)
            {
                if (actualRating == Polarity.Positive)
                    testRatingAnalyis.correctPositive++;
                else if (actualRating == Polarity.Negative)
                    testRatingAnalyis.correctNegative++;
                else
                    testRatingAnalyis.correctNeutral++;
            }
            else if (actualRating == Polarity.Positive)
            {
                testRatingAnalyis.falsePositive++;
                if (expectedRating == Polarity.Negative)
                    testRatingAnalyis.shouldNegativeButIsPositive++;
                else if (expectedRating == Polarity.Neutral)
                    testRatingAnalyis.shouldNeutralButIsPositive++;
            }
            else if (actualRating == Polarity.Negative)
            {
                testRatingAnalyis.falseNegative++;
                if (expectedRating == Polarity.Positive)
                    testRatingAnalyis.shouldPositiveButIsNegative++;
                else if (expectedRating == Polarity.Neutral)
                    testRatingAnalyis.shouldNeutralButIsNegative++;
            }
            else if (actualRating == Polarity.Neutral)
            {
                testRatingAnalyis.falseNeutral++;
                if (expectedRating == Polarity.Positive)
                    testRatingAnalyis.shouldPositiveButIsNeutral++;
                else if (expectedRating == Polarity.Negative)
                    testRatingAnalyis.shouldNegativeButIsNeutral++;
            }
            //else if (actualRating == Equal)
            //{
            //    testRatingAnalyis.indecisive++;
            //    if (expectedRating == Positive)
            //        testRatingAnalyis.shouldPositiveButIsEqual++;
            //    else if (expectedRating == Negative)
            //        testRatingAnalyis.shouldNegativeButIsEqual++;
            //    else
            //        testRatingAnalyis.shouldNeutralButIsEqual++;
            //}
        }

        private float getPercentage(int part, int total)
        {
            return ((float)part) / total * 100;
        }

        private int getCountWithPolarity(List<Tweet> tweets, Polarity polarity)
        {
            return tweets.Count(t => t.annotatedPolarity == polarity);
        }

        private void calculatePrecisionValues(ref TestRatingAnalyis testRatingAnalyis)
        {
            int positiveRatedTweetsCount = testRatingAnalyis.correctPositive + testRatingAnalyis.falsePositive;
            int negativeRatedTweetsCount = testRatingAnalyis.correctNegative + testRatingAnalyis.falseNegative;
            int neutralRatedTweetsCount = testRatingAnalyis.correctNeutral + testRatingAnalyis.falseNeutral;

            testRatingAnalyis.precisionPositive = ((float)testRatingAnalyis.correctPositive) / positiveRatedTweetsCount;
            testRatingAnalyis.precisionNegative = ((float)testRatingAnalyis.correctNegative) / negativeRatedTweetsCount;
            testRatingAnalyis.precisionNeutral = ((float)testRatingAnalyis.correctNeutral) / neutralRatedTweetsCount;
        }

        private void calculateRecallValues(List<Tweet> tweets, ref TestRatingAnalyis testRatingAnalyis)
        {
            int totalPositiveTweetsCount = getCountWithPolarity(tweets, Polarity.Positive);
            int totalNegativeTweetsCount = getCountWithPolarity(tweets, Polarity.Negative);
            int totalNeutralTweetsCount = getCountWithPolarity(tweets, Polarity.Neutral);

            testRatingAnalyis.recallPositive = ((float)testRatingAnalyis.correctPositive) / totalPositiveTweetsCount;
            testRatingAnalyis.recallNegative = ((float)testRatingAnalyis.correctNegative) / totalNegativeTweetsCount;
            testRatingAnalyis.recallNeutral = ((float)testRatingAnalyis.correctNeutral) / totalNeutralTweetsCount;
        }

        private void calculateF1Score(ref TestRatingAnalyis testRatingAnalyis)
        {
            testRatingAnalyis.f1ScorePositive = 2 * (testRatingAnalyis.precisionPositive * testRatingAnalyis.recallPositive)
                / (testRatingAnalyis.precisionPositive + testRatingAnalyis.recallPositive);
            testRatingAnalyis.f1ScoreNegative = 2 * (testRatingAnalyis.precisionNegative * testRatingAnalyis.recallNegative)
                / (testRatingAnalyis.precisionNegative + testRatingAnalyis.recallNegative);
            testRatingAnalyis.f1ScoreNeutral = 2 * (testRatingAnalyis.precisionNeutral * testRatingAnalyis.recallNeutral)
                / (testRatingAnalyis.precisionNeutral + testRatingAnalyis.recallNeutral);
        }
    }

    public struct TestRatingAnalyis
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

        public float precisionPositive;
        public float precisionNegative;
        public float precisionNeutral;

        public float recallPositive;
        public float recallNegative;
        public float recallNeutral;

        public float f1ScorePositive;
        public float f1ScoreNegative;
        public float f1ScoreNeutral;

        public TestRatingAnalyis(int a)
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

            precisionPositive = 0;
            precisionNegative = 0;
            precisionNeutral = 0;

            recallPositive = 0;
            recallNegative = 0;
            recallNeutral = 0;

            f1ScorePositive = 0;
            f1ScoreNegative = 0;
            f1ScoreNeutral = 0;
        }
    }
}