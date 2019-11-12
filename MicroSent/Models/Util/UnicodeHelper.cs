using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MicroSent.Models.Util
{
    public class UnicodeHelper
    {
        private static Regex emojiVarationDetection = new Regex(@"0[0-9A-F]-FE"); //from FE00 to FE0F but little endian

        public static bool isEmojiVariationSelector(string text)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text);
            string hex = BitConverter.ToString(bytes); // THIS IS LITTLE ENDIAN (!)

            return emojiVarationDetection.Match(hex).Success;
        }
    }
}
