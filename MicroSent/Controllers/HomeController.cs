using LinqToTwitter;
using MicroSent.Models;
using MicroSent.Models.Analyser;
using MicroSent.Models.Constants;
using MicroSent.Models.Network;
using MicroSent.Models.Test;
using MicroSent.Models.TwitterConnection;
using MicroSent.Models.Util;
using MicroSent.Models.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OpenNLP.Tools.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSent.Controllers
{
    public class HomeController : Controller
    {
        private TwitterCrawler twitterCrawler;

        private Tokenizer tokenizer;
        private TokenAnalyser tokenAnalyser;
        private TweetAnalyser tweetAnalyser;
        private WordRater wordRater;
        private PosTagger posTagger;
        private SentimentCalculator sentimentCalculator;
        private Preprocessor preprocessor;
        private ParseTreeAnalyser parseTreeAnalyser;

        private NetworkClientSocket networkSendClientSocket;
        private NetworkClientSocket networkReceiveClientSocket;

        private Serializer serializer;
        private Deserializer deserializer;

        private Tester tester;

        private const int NetworkSendClientPort = 6048;
        private const int NetworkReceiveClientPort = 6050;
        private const string NetworkClientHost = "localhost";

        private const string SerializedTweetsPath = "data/testtweets.bin";

        /////////////////////////////////////////////////////////////////////////////////////
        /// CONFIGURATION

        private bool testing = true;
        private bool useGoogleParser = true;
        private bool useSerializedData = true;
        private bool serializeData = false;

        private bool intensifyLastSentence = false;

            //emojis:
        private int minimalOccurences = 100;
        private float minimalPositiveScore = .5f;
        private float minimalNegativeScore = .4f;

        /////////////////////////////////////////////////////////////////////////////////////

        public HomeController(IOptions<TwitterCrawlerConfig> config)
        {
            generateEmojiRegexStrings();

            posTagger = new PosTagger();
            twitterCrawler = new TwitterCrawler(config);
            tokenizer = new Tokenizer();
            tokenAnalyser = new TokenAnalyser();
            tweetAnalyser = new TweetAnalyser();
            wordRater = new WordRater();
            sentimentCalculator = new SentimentCalculator();
            preprocessor = new Preprocessor();
            parseTreeAnalyser = new ParseTreeAnalyser();

            networkSendClientSocket = new NetworkClientSocket(NetworkSendClientPort, NetworkClientHost);
            networkReceiveClientSocket = new NetworkClientSocket(NetworkReceiveClientPort, NetworkClientHost);

            serializer = new Serializer();
            deserializer = new Deserializer();

            tester = new Tester();
        }

        public async Task<IActionResult> Index()
        {
            List<Tweet> allTweets = new List<Tweet>();
            if (useSerializedData)
            {
                allTweets = deserializer.deserializeTweets(SerializedTweetsPath);
            }
            else if (testing)
            {
                allTweets = tester.getTestTweets().Skip(490).ToList();
            }
            else
            {
                //allTweets = await getTweetsAsync();

                //Tweet tw = new Tweet("@Men is so under control. Is this not cool? He's new #new #cool #wontbeveryinteresting", "aa", 0);
                //Tweet tw = new Tweet("This is not a simple english sentence to understand the parser further.", "aa", 0);
                Tweet tw = new Tweet("GO @ok_remi_ok GO! ❤️ https://t.co/3uV7QB8A5r", "aa", 0);
                allTweets.Add(tw);
            }

            foreach (Tweet tweet in allTweets)
            {
                ConsolePrinter.printAnalysisStart(allTweets, tweet);
                if (!useSerializedData || !useGoogleParser)
                {
                    tweet.fullText = preprocessor.replaceAbbrevations(tweet.fullText);

                    //////////////////////////////////////////////////////////////
                    /// TEST AREA
                    //if (tweet.fullText.Contains("That didn't work out very well.")) //(tweet.fullText.StartsWith("Please @msexcel, don't be jealous."))
                    if (tweet.fullText.Contains("GO"))
                    {
                        int a = 0;
                    }
                    //////////////////////////////////////////////////////////////

                    List<Token> allTokens = tokenizer.splitIntoTokens(tweet);
                    tweet.tokenCount = allTokens.Count;

                    foreach (Token token in allTokens)
                    {
                        //single Token analysis

                        tokenAnalyser.analyseTokenType(token);
                        if (token.isHashtag)
                            tokenAnalyser.splitHashtag(token);
                        tokenAnalyser.checkForUppercase(token);
                        tokenAnalyser.convertToLowercase(token);
                        if (!token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
                        {
                            tokenAnalyser.removeRepeatedLetters(token);
                            tokenAnalyser.stem(token);
                        }
                    }

                    //single tweet analysis
                    tweetAnalyser.analyseFirstEndHashtagPosition(allTokens, tweet);
                    posTagger.cutIntoSentences(tweet, allTokens);

                    if (useGoogleParser)
                    {
                        for (int i = 0; i < tweet.sentences.Count; i++)
                        {
                            networkSendClientSocket.sendStringToServer(tweet.getFullSentence(i));
                            Task<string> serverAnswere = networkReceiveClientSocket.receiveParseTree();

                            await serverAnswere;
                            JObject treeJSON = JObject.Parse(serverAnswere.Result);

                            JArray tokens = treeJSON.Value<JArray>(GoogleParserConstants.TOKEN_ARRAY);
                            parseTreeAnalyser.buildTreeFromGoogleParser(tweet, tokens, i);
                        }
                    }
                    else
                    {
                        foreach (var sentence in tweet.sentences)
                        {
                            Parse tree = posTagger.parseTweet(sentence);
                            Node rootNode = parseTreeAnalyser.translateToNodeTree(tree, tweet);
                            tweet.parseTrees.Add(rootNode);
                        }
                    }
                }


                if (!serializeData)
                {
                    tweetAnalyser.filterUselessInterogativeSentences(tweet);

                    //parseTreeAnalyser.applyGoogleParseTreeNegation(tweet);
                    //tweetAnalyser.applyParseTreeDependentNegation(tweet, true);
                    tweetAnalyser.applyKWordNegation(tweet, NegationConstants.FOUR_WORDS);
                    tweetAnalyser.applySpecialStructureNegation(tweet);

                    tweetAnalyser.applyEndHashtagNegation(tweet);

                    applyRating(tweet);

                    sentimentCalculator.calculateFinalSentiment(tweet, intensifyLastSentence: intensifyLastSentence);
                }
            }

            if(serializeData)
                serializer.serializeTweets(allTweets, SerializedTweetsPath);


            if (testing)
                tester.checkTweetRating(allTweets);
            else
                printOnConsole(allTweets);

            return View();
        }

        private async Task<List<Tweet>> getTweetsAsync()
        {
            List<Tweet> allTweets = new List<Tweet>();
            List<Status> quotedRetweetStatuses = new List<Status>();
            List<Status> linkStatuses = new List<Status>();
            quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("AlanZucconi");
            //linkStatuses = await twitterCrawler.getLinks("AlanZucconi");
            //List<Status> ironyHashtags = await twitterCrawler.searchFor("#irony", 200);
            //quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets("davidkrammer");

            foreach(Status status in quotedRetweetStatuses)
            {
                Tweet tweet = new Tweet(status.FullText, status.ScreenName, status.StatusID);
                allTweets.Add(tweet);
            }

            return allTweets;
        }

        private void applyRating(Tweet tweet)
        {
            foreach (List<Token> sentence in tweet.sentences)
            {
                foreach (Token token in sentence)
                {
                    if (!token.isLink && !token.isMention && !token.isPunctuation && !token.isStructureToken)
                    {
                        token.wordRating = wordRater.getWordRating(token, useOnlyAverageScore: true);
                    }
                }
            }

            foreach(Token token in tweet.rest)
            {
                if (token.isEmoji)
                {
                    token.emojiRating = wordRater.getEmojiRating(token);
                }
                else if (token.isSmiley)
                {
                    //TODO
                }
            }
        }

        #region emoji regex string generation
        private void generateEmojiRegexStrings()
        {
            var allEmojis = loadAllRelevantEmojis();
            var allRelevantEmojis = allEmojis.Where(e => e.occurences >= minimalOccurences
                && (e.positiveScore >= minimalPositiveScore
                || e.negativeScore >= minimalNegativeScore)).ToList();
            var allPositiveEmojis = allRelevantEmojis.Where(e => e.positiveScore >= minimalPositiveScore).ToList();
            var allNegativeEmojis = allRelevantEmojis.Where(e => e.negativeScore >= minimalNegativeScore).ToList();
            string allEmojiRegex = getEmojiRegexString(allRelevantEmojis);
            string positiveEmojiRegex = getEmojiRegexString(allPositiveEmojis);
            string negativeEmojiRegex = getEmojiRegexString(allNegativeEmojis);

            RegexConstants.ALL_EMOTICON_DETECTION = allEmojiRegex;
            RegexConstants.POSITIVE_EMOTICON_DETECTION = positiveEmojiRegex;
            RegexConstants.NEGATIVE_EMOTICON_DETECTION = negativeEmojiRegex;
        }

        private List<Emoji> loadAllRelevantEmojis()
        {
            Deserializer deserializer = new Deserializer("emojis", "data/emojis.xml", typeof(List<Emoji>));
            deserializer.deserializeEmojiList(out List<Emoji> emojis);
            return emojis;
        }

        private string getEmojiRegexString(List<Emoji> emojis)
        {
            string regexString = $"{emojis.First().unicodeCharacter}";
            foreach (Emoji emoji in emojis.Skip(1))
            {
                regexString += $"|{emoji.unicodeCharacter}";
            }
            return regexString;
        }
        #endregion

        private void printOnConsole(List<Tweet> allTweets)
        {
            foreach (Tweet tweet in allTweets)
            {
                ConsolePrinter.printTweetAnalysisHead(tweet);
                ConsolePrinter.printPositiveRating(tweet);
                ConsolePrinter.printEmptyLine();
                ConsolePrinter.printNegativeRating(tweet);
                ConsolePrinter.printEmptyLine();
            }
        }
    }
}
