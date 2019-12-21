using MicroSent.Models;
using MicroSent.Models.Analyser;
using MicroSent.Models.Enums;
using MicroSent.Models.Serialization;
using OpenNLP.Tools.Parser;
using OpenNLP.Tools.PosTagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace LexiconExtension
{
    class LexiconExtender
    {
        private const string SentWordLabelAdjective = "a";
        private const string SentWordLabelNoun = "n";
        private const string SentWordLabelAdverb = "r";
        private const string SentWordLabelVerb = "v";

        //Dictionary<string, List<float>> lexiconExtension = new Dictionary<string, List<float>>();
        private List<Word> wordList = new List<Word>();
        private Dictionary<string, Word> wordDict = new Dictionary<string, Word>();
        private int positiveTweetsCount;
        private int negativeTweetsCount;

        public void extract()
        {
            loadLexicon(out Dictionary<string, float> polarityLexicon);
            
            //polarityLexicon = polarityLexicon.Where(kv => kv.Value != 0).ToDictionary(kv => kv.Key, kv => kv.Value);
            buildWordList(polarityLexicon);
            //final calculation
            //Dictionary<string, float> finalScores = calculateFinalScore();
            calculateChiSquareTest();
            var significantWord = wordList.Where(w => w.chiSquareValue >= 6.63d).OrderByDescending(w => w.chiSquareValue).ToList();
            //serialization
            serializeData("LexiconExtension", @"data\lexiconExtensionAll.xml", wordList);
            serializeData("LexiconExtension", @"data\lexiconExtension.xml", significantWord);

            int a = 0;
        }

        private void buildWordList(Dictionary<string, float> polarityLexicon)
        {
            Tokenizer tokenizer = new Tokenizer();
            TokenAnalyser tokenAnalyser = new TokenAnalyser();
            PosTagger posTagger = new PosTagger();
            loadTrainingTweets(out List<Item> trainingTweets);
            List<Item> selectedTweets = trainingTweets.Where(t => t.value == 4).Skip(0).ToList();
            selectedTweets.AddRange(trainingTweets.Where(t => t.value == 0).Skip(0).ToList());
            trainingTweets = selectedTweets;
            positiveTweetsCount = trainingTweets.Count(t => t.value == 4);
            negativeTweetsCount = trainingTweets.Count(t => t.value == 0);

            ulong id = 0;

            foreach (Item tweetItem in trainingTweets)
            {
                //if (trainingTweets.IndexOf(tweetItem) > 20000)
                //    break;

                Console.WriteLine($"Analyzing tweet {id + 1} of {trainingTweets.Count}");

                Tweet tweet = new Tweet(tweetItem.key, "", id);
                Polarity tweetPolarity = translateItemPolarity(tweetItem);
                List<string> alreadyAnalysedLexiconKeys = new List<string>();
                var tokens = tokenizer.splitIntoTokens(tweet);
                foreach (Token token in tokens)
                {
                    tokenAnalyser.analyseTokenType(token);
                }
                posTagger.cutIntoSentences(tweet, tokens);
                posTagger.tagAllTokens(tweet);

                foreach (Token token in tokens.Where(t => !t.isLink && !t.isPunctuation && !t.isStructureToken && !t.isMention))
                {
                    tokenAnalyser.convertToLowercase(token);
                    analyseToken(token, tweetPolarity, alreadyAnalysedLexiconKeys, polarityLexicon);
                }
                id++;
            }
        }

        private void calculateChiSquareTest()
        {
            foreach(Word word in wordList)
            {
                long positiveTweetCountWithoutWord = positiveTweetsCount - word.positiveOccurences;
                long negativeTweetCountWithoutWord = negativeTweetsCount - word.negativeOccurences;
                
                long tweetsWithWordCount = word.positiveOccurences + word.negativeOccurences;
                long tweetsWithoutWordCount = positiveTweetsCount + negativeTweetsCount - tweetsWithWordCount;

                double allTweetsCount = positiveTweetsCount + negativeTweetsCount;
                
                double expected11 = (positiveTweetsCount * tweetsWithWordCount) / allTweetsCount;
                double expected12 = ((positiveTweetsCount * tweetsWithoutWordCount)) / allTweetsCount;
                double expected21 = (negativeTweetsCount * tweetsWithWordCount) / allTweetsCount;
                double expected22 = (negativeTweetsCount * tweetsWithoutWordCount) / allTweetsCount;
                
                double chiSquare11 = (float)(Math.Pow((word.positiveOccurences - expected11), 2)) / (expected11);
                double chiSquare12 = (float)(Math.Pow((positiveTweetCountWithoutWord - expected12), 2)) / (expected12);
                double chiSquare21 = (float)(Math.Pow((word.negativeOccurences - expected21), 2)) / (expected21);
                double chiSquare22 = (float)(Math.Pow((negativeTweetCountWithoutWord - expected22), 2)) / (expected22);

                word.chiSquareValue = chiSquare11 + chiSquare12 + chiSquare21 + chiSquare22;
            }
        }

        private void analyseToken(Token token, Polarity polarity, List<string> alreadyAnalysedLexiconKeys,
            Dictionary<string, float> polarityLexicon)
        {
            string lexiconKey = getLexiconKey(token);

            //the standard lexicon only recognizes verbs/nouns/adverbs/adjectives
            //this will be kept for the lexicon extension
            if (lexiconKey != null && !polarityLexicon.ContainsKey(lexiconKey))
            {
                Word word;
                if (!wordDict.ContainsKey(lexiconKey))
                {
                    word = new Word(lexiconKey);
                    wordList.Add(word);
                    wordDict.Add(lexiconKey, word);
                }
                else
                {
                    word = wordDict[lexiconKey];
                }

                switch (polarity)
                {
                    case Polarity.Positive:
                        if (alreadyAnalysedLexiconKeys.Contains(lexiconKey))
                            return;
                        word.positiveOccurences++;
                        alreadyAnalysedLexiconKeys.Add(lexiconKey);
                        break;
                    case Polarity.Negative:
                        if (alreadyAnalysedLexiconKeys.Contains(lexiconKey))
                            return;
                        word.negativeOccurences++;
                        alreadyAnalysedLexiconKeys.Add(lexiconKey);
                        break;
                    default:
                        int a = 0;
                        break;
                }
            }
        }

        private Polarity translateItemPolarity(Item item)
        {
            switch (item.value)
            {
                case 0:
                    return Polarity.Negative;
                case 4:
                    return Polarity.Positive;
            }
            return Polarity.Neutral;
        }


        //private void analyseSentences(Tweet tweet, Dictionary<string, float> polarityLexicon)
        //{
        //    foreach (List<Token> sentence in tweet.sentences)
        //    {
        //        List<int> sentenceIndexesOfTokensInLexicon = new List<int>();
        //        foreach (Token token in sentence)
        //        {
        //            string key = getLexiconKey(token);
        //            if (polarityLexicon.ContainsKey(key)
        //                && polarityLexicon[key] != 0)
        //            {
        //                sentenceIndexesOfTokensInLexicon.Add(token.indexInSentence);
        //            }
        //        }
        //        if (sentenceIndexesOfTokensInLexicon.Count > 0)
        //        {
        //            calculateDistance(sentence, sentenceIndexesOfTokensInLexicon, polarityLexicon);
        //        }
        //    }
        //}

        //private void calculateDistance(List<Token> sentence, List<int> sentenceIndexesOfTokensInLexicon, Dictionary<string, float> polarityLexicon)
        //{
        //    foreach(int tokenIndex in sentenceIndexesOfTokensInLexicon)
        //    {
        //        Token referenceToken = sentence.Where(t => t.indexInSentence == tokenIndex).First();
        //        foreach(Token token in sentence)
        //        {
        //            if (sentenceIndexesOfTokensInLexicon.Contains(token.indexInSentence)
        //                || convertToSentiWordPosLabel(token.posLabel) == null
        //                || polarityLexicon.ContainsKey(getLexiconKey(token)))
        //                continue;

        //            int distance = Math.Abs(token.indexInSentence - tokenIndex);
        //            float rating = polarityLexicon[getLexiconKey(referenceToken)];
        //            float score = calculateEntityScore(distance, rating);

        //            extendLexicon(token, score);
        //        }
        //    }
        //}

        //private Dictionary<string, float> calculateFinalScore()
        //{
        //    Dictionary<string, float> finalScores = new Dictionary<string, float>();
        //    var keys = lexiconExtension.Keys;
        //    foreach(var key in keys)
        //    {
        //        List<float> scores = lexiconExtension[key];
        //        float finalScore = scores.Sum() / scores.Count;
        //        finalScores.Add(key, finalScore);
        //    }
        //    return finalScores;
        //}

        //private void extendLexicon(Token token, float score)
        //{
        //    string key = getLexiconKey(token);
        //    if (lexiconExtension.ContainsKey(key))
        //    {
        //        lexiconExtension[key].Add(score);
        //    }
        //    else
        //    {
        //        lexiconExtension.Add(key, new List<float>() { score });
        //    }
        //}

        private void serializeData(string rootName, string outputPath, List<Word> selectedWords)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<Word>), new XmlRootAttribute() { ElementName = rootName });
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                xmlSerializer.Serialize(writer, selectedWords);
            }
        }

        private float calculateEntityScore(int distance, float rating)
        {
            return rating / distance;
        }

        private string getLexiconKey(Token token)
        {
            string posType = convertToSentiWordPosLabel(token.posLabel);
            if (posType != null)
                return $"{token.text}!{posType}";
            else
                return null;
        }

        private void loadLexicon(out Dictionary<string, float> polarityLexicon)
        {
            Deserializer deserializer = new Deserializer("SentiWords", "data/polarityLexicon.xml", typeof(Item[]));
            deserializer.deserializeDictionary(out polarityLexicon);
        }

        private void loadTrainingTweets(out List<Item> trainingTweets)
        {
            Deserializer deserializer = new Deserializer("TrainingData", "data/trainingData.xml", typeof(Item[]));
            deserializer.deserializeList(out trainingTweets);
        }

        private string convertToSentiWordPosLabel(PosLabels label)
        {
            switch (label)
            {
                case PosLabels.JJ:
                case PosLabels.JJR:
                case PosLabels.JJS:
                    return SentWordLabelAdjective;
                case PosLabels.NN:
                case PosLabels.NNP:
                case PosLabels.NNPS:
                case PosLabels.NNS:
                    return SentWordLabelNoun;
                case PosLabels.RB:
                case PosLabels.RBR:
                case PosLabels.RBS:
                    return SentWordLabelAdverb;
                case PosLabels.VB:
                case PosLabels.VBD:
                case PosLabels.VBG:
                case PosLabels.VBN:
                case PosLabels.VBP:
                case PosLabels.VBZ:
                    return SentWordLabelVerb;
            }
            return null;
        }
    }
}
