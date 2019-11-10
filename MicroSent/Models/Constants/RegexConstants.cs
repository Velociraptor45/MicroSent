namespace MicroSent.Models.Constants
{
    public class RegexConstants
    {
        public const string LINK_DETECTION = @"(https?:\/\/(www\.)?|www\.)([\d\w]+[\.\/])+[\d\w\?\=]+";
        //public static string SMILEY_DETECTION = @"(:-?|=)(\)|\(|\||\/|(D\b))|\bD:|: [\)\(]";
        public static string ALL_SMILEY_DETECTION = "";
        public static string POSITIVE_SMILEY_DETECTION = "";
        public static string NEGATIVE_SMILEY_DETECTION = "";
        public const string PUNCTUATION_DETECTION = @"[\?!]+|\.+|,|:";
        public const string WORDS_DETECTION = @"(@|#[a-z]|\\|/)?(\w([''-]\w)?)+";
        public const string SENTENCE_STRUCTURE_DETECTION = @"(\(|\)|-|""|'')";
        public static string ALL_EMOJI_DETECTION = "";
        public static string POSITIVE_EMOJI_DETECTION = "";
        public static string NEGATIVE_EMOJI_DETECTION = "";

        private const string NegationWordsBeginning = @"(ai|are|ca|could|did|does|do|had|has|have|is|must|need|ought|shall|should|was|were|wo|would)";
        public static readonly string NEGATION_WORD_DETECTION = $@"\bcannot|{NegationWordsBeginning}n'?t\b";
        public static readonly string NEGATION_HASHTAG_DETECTION = $@"\bno(t|n)?\b|\bnever\b|{NegationWordsBeginning}nt\b";
        public const string NEGATION_TOKEN_DETECTION = @"\bno(t|n-?)?\b|\bnever\b|\bn'?t\b";
    }
}
