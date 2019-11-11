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
using MicroSent.Models.Enums;

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

        private const string SerializedTweetsPath = DataPath.SERIALIZED_TWEETS;

        /////////////////////////////////////////////////////////////////////////////////////
        /// CONFIGURATION

        private bool testing = true;
        private bool useGoogleParser = true;
        private bool useSerializedData = true;
        private bool serializeData = false;

        private bool intensifyLastSentence = false;

        private int skipTweetsAmount = 0;

            //emojis:
        private int minimalOccurences = 100;
        private float minimalPositiveScore = .5f;
        private float minimalNegativeScore = .4f;

        /////////////////////////////////////////////////////////////////////////////////////

        public HomeController(IOptions<TwitterCrawlerConfig> config)
        {
            generateEmojiRegexStrings();
            generateSmileyRegexStrings();

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
                allTweets = tester.getTestTweets().Skip(skipTweetsAmount).ToList();
            }
            else
            {
                //allTweets = await getTweetsAsync("AlanZucconi");

                //Tweet tw = new Tweet("@Men is so under control. Is this not cool? He's new #new #cool #wontbeveryinteresting", "aa", 0);
                //Tweet tw = new Tweet("This is not a simple english sentence to understand the parser further.", "aa", 0);
                Tweet tw = new Tweet("You are so GREAT! :)", "aa", 0);
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

                    //////////////////////////////////////////////////////////////
                    /// NEGATION
                    //parseTreeAnalyser.applyGoogleParseTreeNegation(tweet);
                    //tweetAnalyser.applyParseTreeDependentNegation(tweet, true);
                    tweetAnalyser.applyKWordNegation(tweet, NegationConstants.FOUR_WORDS);
                    tweetAnalyser.applySpecialStructureNegation(tweet);
                    tweetAnalyser.applyEndHashtagNegation(tweet);
                    //////////////////////////////////////////////////////////////

                    tweetAnalyser.checkforIrony(tweet);

                    applyRating(tweet);

                    sentimentCalculator.calculateFinalSentiment(tweet, intensifyLastSentence: intensifyLastSentence);
                }
            }

            if(serializeData && !useSerializedData)
                serializer.serializeTweets(allTweets, SerializedTweetsPath);


            if (testing)
                tester.checkTweetRating(allTweets);
            else
                printOnConsole(allTweets);

            return View();
        }

        private async Task<List<Tweet>> getTweetsAsync(string accountName)
        {
            List<Tweet> allTweets = new List<Tweet>();
            List<Status> quotedRetweetStatuses = new List<Status>();
            List<Status> linkStatuses = new List<Status>();

            ConsolePrinter.printBeginCrawlingTweets(accountName);
            quotedRetweetStatuses = await twitterCrawler.getQuotedRetweets(accountName);
            ConsolePrinter.printFinishedCrawlingTweets();
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
                    token.smileyRating = wordRater.getSmileyRating(token);
                }
            }
        }

        #region emoji/smiley regex string generation
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

            RegexConstants.ALL_EMOJI_DETECTION = allEmojiRegex;
            RegexConstants.POSITIVE_EMOJI_DETECTION = positiveEmojiRegex;
            RegexConstants.NEGATIVE_EMOJI_DETECTION = negativeEmojiRegex;
        }

        private void generateSmileyRegexStrings()
        {
            List<Smiley> allSmileys = loadAllSmileys();
            List<Smiley> positiveSmileys = allSmileys.Where(s => s.polarity == Polarity.Positive).ToList();
            List<Smiley> negativeSmileys = allSmileys.Where(s => s.polarity == Polarity.Negative).ToList();
            string allSmileyRegex = getSmileyRegexString(allSmileys);
            string positiveSmileyRegex = getSmileyRegexString(positiveSmileys);
            string negativeSmileyRegex = getSmileyRegexString(negativeSmileys);

            RegexConstants.ALL_SMILEY_DETECTION = allSmileyRegex;
            RegexConstants.POSITIVE_SMILEY_DETECTION = positiveSmileyRegex;
            RegexConstants.NEGATIVE_SMILEY_DETECTION = negativeSmileyRegex;
        }

        private List<Emoji> loadAllRelevantEmojis()
        {
            Deserializer deserializer = new Deserializer("emojis", "data/emojis.xml", typeof(List<Emoji>));
            deserializer.deserializeEmojiList(out List<Emoji> emojis);
            return emojis;
        }

        private List<Smiley> loadAllSmileys()
        {
            Deserializer deserializer = new Deserializer("smileys", "data/smileys.xml", typeof(List<Smiley>));
            deserializer.deserializeSmileyList(out List<Smiley> smileys);
            return smileys;
        }

        private string getSmileyRegexString(List<Smiley> smileys)
        {
            string regexString = $"{escapeRegexCharacters(smileys.First().smiley)}";
            foreach(Smiley smiley in smileys.Skip(1))
            {
                regexString += $"|{escapeRegexCharacters(smiley.smiley)}";
            }
            return regexString;
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

        private string escapeRegexCharacters(string pattern)
        {
            return pattern.Replace(@"\", @"\\").Replace("(", @"\(").Replace(")", @"\)")
                .Replace("{", @"\{").Replace("}", @"\}").Replace("[", @"\[").Replace("]", @"\]")
                .Replace("*", @"\*").Replace("/", @"\/").Replace("^", @"\^").Replace(".", @"\.")
                .Replace("|", @"\|");
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
