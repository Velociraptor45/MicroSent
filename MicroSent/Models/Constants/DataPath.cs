namespace MicroSent.Models.Constants
{
    public class DataPath
    {
        public const string SERIALIZED_TWEETS = @"data/testtweets.bin";

        public const string POLARITY_LEXICON = @"data\wordPolarity\polarityLexicon.xml";

        public const string NHUNSPELL_FOLDER = @"data\nhunspell";
        public static readonly string NHUNSPELL_ENG_AFF = $@"{NHUNSPELL_FOLDER}\en_us.aff";
        public static readonly string NHUNSPELL_ENG_DICT = $@"{NHUNSPELL_FOLDER}\en_us.dic";

        public const string NBIN_FOLDER = @"data\NBIN_files\";

        public const string SLANG_FILE = @"data\slang\slang.txt";

        public const string TEST_DATA = @"data\testdata\testdata.xml";

        public const string EMOJI_FILE = @"data/emojis.xml";
        public const string SMILEY_FILE = @"data/smileys.xml";

        public const string LEMMA_FILE_ENG = @"data/lemmatization/full7z-mlteast-en.lem";
    }
}
