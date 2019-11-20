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

        Dictionary<string, List<float>> lexiconExtension = new Dictionary<string, List<float>>();

        public void extract()
        {
            loadLexicon(out Dictionary<string, float> polarityLexicon);
            //polarityLexicon = polarityLexicon.Where(kv => kv.Value != 0).ToDictionary(kv => kv.Key, kv => kv.Value);
            extractRelevantSentences(polarityLexicon);

            //final calculation
            Dictionary<string, float> finalScores = calculateFinalScore();

            //serialization
            serializeData("LexiconExtension", @"data\extendedLexicon.xml", finalScores);

            int a = 0;
        }

        private void extractRelevantSentences(Dictionary<string, float> polarityLexicon)
        {
            Tokenizer tokenizer = new Tokenizer();
            TokenAnalyser tokenAnalyser = new TokenAnalyser();
            PosTagger posTagger = new PosTagger();
            loadTrainingTweets(out List<Item> trainingTweets);

            foreach (Item tweetItem in trainingTweets)
            {
                if (trainingTweets.IndexOf(tweetItem) > 20000)
                    break;

                Console.WriteLine($"Analyzing tweet {trainingTweets.IndexOf(tweetItem)} of {trainingTweets.Count}");

                Tweet tweet = new Tweet(tweetItem.key, "", 0);
                var tokens = tokenizer.splitIntoTokens(tweet);
                foreach (Token token in tokens)
                {
                    tokenAnalyser.analyseTokenType(token);
                }
                posTagger.cutIntoSentences(tweet, tokens);
                posTagger.tagAllTokens(tweet);

                foreach (Token token in tokens)
                {
                    tokenAnalyser.convertToLowercase(token);
                }

                analyseSentences(tweet, polarityLexicon);
            }
        }


        private void analyseSentences(Tweet tweet, Dictionary<string, float> polarityLexicon)
        {
            foreach (List<Token> sentence in tweet.sentences)
            {
                List<int> sentenceIndexesOfTokensInLexicon = new List<int>();
                foreach (Token token in sentence)
                {
                    string key = getLexiconKey(token);
                    if (polarityLexicon.ContainsKey(key)
                        && polarityLexicon[key] != 0)
                    {
                        sentenceIndexesOfTokensInLexicon.Add(token.indexInSentence);
                    }
                }
                if (sentenceIndexesOfTokensInLexicon.Count > 0)
                {
                    calculateDistance(sentence, sentenceIndexesOfTokensInLexicon, polarityLexicon);
                }
            }
        }

        private void calculateDistance(List<Token> sentence, List<int> sentenceIndexesOfTokensInLexicon, Dictionary<string, float> polarityLexicon)
        {
            foreach(int tokenIndex in sentenceIndexesOfTokensInLexicon)
            {
                Token referenceToken = sentence.Where(t => t.indexInSentence == tokenIndex).First();
                foreach(Token token in sentence)
                {
                    if (sentenceIndexesOfTokensInLexicon.Contains(token.indexInSentence)
                        || convertToSentiWordPosLabel(token.posLabel) == null
                        || polarityLexicon.ContainsKey(getLexiconKey(token)))
                        continue;

                    int distance = Math.Abs(token.indexInSentence - tokenIndex);
                    float rating = polarityLexicon[getLexiconKey(referenceToken)];
                    float score = calculateEntityScore(distance, rating);

                    extendLexicon(token, score);
                }
            }
        }

        private Dictionary<string, float> calculateFinalScore()
        {
            Dictionary<string, float> finalScores = new Dictionary<string, float>();
            var keys = lexiconExtension.Keys;
            foreach(var key in keys)
            {
                List<float> scores = lexiconExtension[key];
                float finalScore = scores.Sum() / scores.Count;
                finalScores.Add(key, finalScore);
            }
            return finalScores;
        }

        private void extendLexicon(Token token, float score)
        {
            string key = getLexiconKey(token);
            if (lexiconExtension.ContainsKey(key))
            {
                lexiconExtension[key].Add(score);
            }
            else
            {
                lexiconExtension.Add(key, new List<float>() { score });
            }
        }

        private void serializeData(string rootName, string outputPath, Dictionary<string, float> finalScores)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Item[]), new XmlRootAttribute() { ElementName = rootName });
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                xmlSerializer.Serialize(writer, finalScores.Select(e => new Item() { key = e.Key, value = e.Value }).ToArray());
            }
        }

        private float calculateEntityScore(int distance, float rating)
        {
            return rating / distance;
        }

        private string getLexiconKey(Token token)
        {
            return $"{token.text}!{convertToSentiWordPosLabel(token.posLabel)}";
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
