using MicroSent.Models.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Util
{
    public class UnicodeHelper
    {
        private static Regex emojiVarationDetection = new Regex(RegexConstants.EMOJI_VARIATION_PATTERN); //from FE00 to FE0F but little endian
        private static int MaxUnicodeNumber = 127;

        public static bool isEmojiVariationSelector(string text)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text);
            string hex = BitConverter.ToString(bytes); // THIS IS LITTLE ENDIAN (!)

            return emojiVarationDetection.Match(hex).Success;
        }

        public static bool isFullWordInUnicode(string word)
        {
            return word.All(c => c <= MaxUnicodeNumber);
        }

        public static string removeNonUnicodeCharacters(string text)
        {
            if (text.Any(c => c > MaxUnicodeNumber))
                return String.Concat(text.Where(c => c <= MaxUnicodeNumber));
            else
                return text;
        }
    }
}
