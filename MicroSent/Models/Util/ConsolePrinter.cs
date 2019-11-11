using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MicroSent.Models.Util
{
    public class ConsolePrinter
    {
        public static bool allowPrinting = true;

        #region tweet crawling
        public static void printBeginCrawlingTweets(string accountName)
        {
            if (allowPrinting)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"######## Start crawling tweets from account {accountName} ########");
                Console.ResetColor();
            }
        }

        public static void printFinishedCrawlingTweets()
        {
            if (allowPrinting)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"######## Finished crawling from Twitter ########");
                Console.ResetColor();
            }
        }
        #endregion

        public static void printEmptyLine()
        {
            if (allowPrinting)
            {
                Console.WriteLine();
            }
        }

        public static void printCorrectedGoogleParsing(string originalSentence)
        {
            if (allowPrinting)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Corrected sentence: {originalSentence}");
                Console.ResetColor();
            }
        }

        public static void printSentenceNotFoundMessage(int sentenceIndex, int tokenSentenceIndexToNegate)
        {
            if (allowPrinting)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Couldn't find token in sentence {sentenceIndex} with index {tokenSentenceIndexToNegate}");
                Console.ResetColor();
            }
        }

        public static void printSentenceIgnored(Tweet tweet, int sentenceIndex)
        {
            if (allowPrinting)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Ignoring sentence \"{tweet.getFullSentence(sentenceIndex)}\"");
                Console.ResetColor();
            }
        }

        #region Hashtag parsing
        public static void startHashtagParsing()
        {
            if (allowPrinting)
            {
                Console.WriteLine("----------------- start analysis --------------------------");
            }
        }

        public static void startForwardHashtagParsing()
        {
            if (allowPrinting)
            {
                Console.WriteLine("Forward parsing:");
            }
        }

        public static void startBackwardHashtagParsing()
        {
            if (allowPrinting)
            {
                Console.WriteLine("Backward parsing:");
            }
        }

        public static void printRestOfHashtagAnalysis(string rest)
        {
            if (allowPrinting)
            {
                Console.WriteLine($"Rest: {rest}");
            }
        }

        public static void printFoundWordInHashtagAnalysis(string word, string hashtag)
        {
            if (allowPrinting)
            {
                Console.WriteLine($"{word} is part of {hashtag}");
            }
        }
        #endregion

        #region processing
        public static void printAnalysisStart(List<Tweet> allTweets, Tweet tweet)
        {
            if (allowPrinting)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("###########################################################################################################");
                Console.WriteLine($"PROCESSING TWEET {allTweets.IndexOf(tweet) + 1}/{allTweets.Count}");
                Console.ResetColor();
            }
        }
        #endregion

        #region final analysis
        public static void printTweetAnalysisHead(Tweet tweet)
        {
            if (allowPrinting)
            {
                Console.WriteLine("_______________________________________________________________");
                Console.WriteLine($"https://twitter.com/{tweet.userScreenName}/status/{tweet.statusID}");
                Console.WriteLine(tweet.fullText);
            }
        }

        public static void printPositiveRating(Tweet tweet)
        {
            if (allowPrinting)
            {
                Console.WriteLine($"Positive Rating: {tweet.positiveRating}");
                foreach (Token token in tweet.sentences.SelectMany(s => s).Where(t => t.totalRating > 0))
                {
                    Console.Write(token.text + $"({token.totalRating}), ");
                }
            }
        }

        public static void printNegativeRating(Tweet tweet)
        {
            if (allowPrinting)
            {
                Console.WriteLine($"Negative Rating: {tweet.negativeRating}");
                foreach (Token token in tweet.sentences.SelectMany(s => s).Where(t => t.totalRating < 0))
                {
                    Console.Write(token.text + $"({token.totalRating}), ");
                }
            }
        }
        #endregion

        #region network communication
        public static void printConnectionEstablished(string host, int port, bool sendingStream)
        {
            if (allowPrinting)
            {
                string type = sendingStream ? "sending" : "receiving";
                Console.WriteLine($"Connected to {host}:{port} ({type} stream)");
            }
        }

        public static void printConnectionClosed(string host, int port)
        {
            if (allowPrinting)
            {
                Console.WriteLine($"Closing connection to {host}:{port}");
            }
        }

        public static void printServerResponseOK()
        {
            if (allowPrinting)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Server response: OK");
                Console.ResetColor();
            }
        }

        public static void printNoServerResponse()
        {
            if (allowPrinting)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No server response - will try again");
                Console.ResetColor();
            }
        }

        public static void printServerConnectionFailed(string host, int port, SocketException e)
        {
            if (allowPrinting)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to connect to {host}:{port}:");
                Console.WriteLine($"{e.StackTrace}");
                Console.ResetColor();
            }
        }

        public static void printSendingMessage(string host, int port)
        {
            if (allowPrinting)
            {
                Console.WriteLine($"Sending message to {host}:{port}");
            }
        }

        public static void printMessageSuccessfullySent(string host, int port)
        {
            if (allowPrinting)
            {
                Console.WriteLine($"Message successfully sent to {host}:{port}");
            }
        }

        public static void printMessageSendingFailed(string host, int port, Exception e)
        {
            if (allowPrinting)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception occured wile sending message to {host}:{port}:");
                Console.WriteLine($"{e.StackTrace}");
                Console.ResetColor();
            }
        }
        #endregion
    }
}
