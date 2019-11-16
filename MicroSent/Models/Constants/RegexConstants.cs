namespace MicroSent.Models.Constants
{
    public class RegexConstants
    {
        public const string LINK_PATTERN = @"(https?:\/\/(www\.)?|www\.)([\d\w]+[\.\/])+[\d\w\?\=]+";
        public static string ALL_SMILEY_PATTERN = "";
        public static string POSITIVE_SMILEY_PATTERN = "";
        public static string NEGATIVE_SMILEY_PATTERN = "";
        public const string PUNCTUATION_PATTERN = @"[\?!]+|\.+|,|:";
        public const string WORDS_PATTERN = @"(@|#[a-z]|\\|/)?(\w([''-]\w)?)+";
        public const string SENTENCE_STRUCTURE_PATTERN = @"(\(|\)|-|""|'')";
        public const string ALL_EMOJI_PATTERN = @"\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff]";
        public static string POSITIVE_EMOJI_PATTERN = "";
        public static string NEGATIVE_EMOJI_PATTERN = "";
        public const string EMOJI_VARIATION_PATTERN = @"0[0-9A-F]-FE";

        private const string NegationWordsBeginning = @"(ai|are|ca|could|did|does|do|had|has|have|is|must|need|ought|shall|should|was|were|wo|would)";
        public static readonly string NEGATION_WORD_PATTERN = $@"\bcannot|{NegationWordsBeginning}n'?t\b";
        public static readonly string NEGATION_HASHTAG_PATTERN = $@"\bno(t|n)?\b|\bnever\b|{NegationWordsBeginning}nt\b";
        public const string NEGATION_TOKEN_PATTERN = @"\bno(t|n-?)?\b|\b(never|barely|hardly)\b|\bn'?t\b";

        public const string DOMAIN_PATTERN = @"\w+\.(\w+|co\.uk)$";
    }
}
