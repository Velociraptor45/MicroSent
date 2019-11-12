using MicroSent.Models.Constants;

namespace MicroSent.Models.Configuration
{
    public class MicroSentConfiguration : IAlgorithmConfiguration
    {
        public bool testing { get; }
        public bool useGoogleParser { get; }
        public bool useSerializedData { get; }
        public bool serializeData { get; }


        public bool intensifyLastSentence { get; }
        public int skipTweetsAmount { get; }


        public int minimalEmojiOccurences { get; }
        public float minimalPositiveEmojiScore { get; }
        public float minimalNegativeEmojiScore { get; }


        public int clientSendingPort { get; }
        public int clientReceivingPort { get; }
        public string clientHost { get; }


        public bool useOnlyAverageRatingScore { get; }
        public bool useSingleTokenThreshold { get;}
        public bool useTotalThreshold { get; }
        public float singleTokenThreshold { get; }
        public float totalThreshold { get; }


        public int negationWindowSize { get; }

        public MicroSentConfiguration()
        {
            testing = true;
            useGoogleParser = true;
            useSerializedData = true;
            serializeData = false;
            skipTweetsAmount = 0;

            intensifyLastSentence = false;

            minimalEmojiOccurences = 100;
            minimalPositiveEmojiScore = .5f;
            minimalNegativeEmojiScore = .4f;

            clientSendingPort = 6048;
            clientReceivingPort = 6050;
            clientHost = "localhost";

            useOnlyAverageRatingScore = false;
            useSingleTokenThreshold = true;
            useTotalThreshold = true;
            singleTokenThreshold = .25f;
            totalThreshold = .5f;

            negationWindowSize = NegationConstants.FOUR_WORDS;
        }
    }
}
