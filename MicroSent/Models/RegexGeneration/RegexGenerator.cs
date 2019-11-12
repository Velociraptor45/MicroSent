using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using MicroSent.Models.Serialization;
using MicroSent.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroSent.Models.RegexGeneration
{
    public class RegexGenerator
    {
        private IAlgorithmConfiguration configuration;

        private string SmileySerializationRootName = "smileys";
        private string EmojiSerializationRootName = "emojis";

        public RegexGenerator(IAlgorithmConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void generateEmojiRegexStrings()
        {
            var allEmojis = loadAllRelevantEmojis();
            var allRelevantEmojis = allEmojis.Where(e => e.occurences >= configuration.minimalEmojiOccurences
                && (e.positiveScore >= configuration.minimalPositiveEmojiScore
                || e.negativeScore >= configuration.minimalNegativeEmojiScore)).ToList();
            var allPositiveEmojis = allRelevantEmojis
                .Where(e => e.positiveScore >= configuration.minimalPositiveEmojiScore).ToList();
            var allNegativeEmojis = allRelevantEmojis
                .Where(e => e.negativeScore >= configuration.minimalNegativeEmojiScore).ToList();
            string allEmojiRegex = getEmojiRegexString(allRelevantEmojis);
            string positiveEmojiRegex = getEmojiRegexString(allPositiveEmojis);
            string negativeEmojiRegex = getEmojiRegexString(allNegativeEmojis);

            RegexConstants.ALL_EMOJI_DETECTION = allEmojiRegex;
            RegexConstants.POSITIVE_EMOJI_DETECTION = positiveEmojiRegex;
            RegexConstants.NEGATIVE_EMOJI_DETECTION = negativeEmojiRegex;
        }

        public void generateSmileyRegexStrings()
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
            Deserializer deserializer = new Deserializer(EmojiSerializationRootName, DataPath.EMOJI_FILE, typeof(List<Emoji>));
            deserializer.deserializeEmojiList(out List<Emoji> emojis);
            return emojis;
        }

        private List<Smiley> loadAllSmileys()
        {
            Deserializer deserializer = new Deserializer(SmileySerializationRootName, DataPath.SMILEY_FILE, typeof(List<Smiley>));
            deserializer.deserializeSmileyList(out List<Smiley> smileys);
            return smileys;
        }

        private string getSmileyRegexString(List<Smiley> smileys)
        {
            string regexString = $"{escapeRegexCharacters(smileys.First().smiley)}";
            foreach (Smiley smiley in smileys.Skip(1))
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
    }
}
