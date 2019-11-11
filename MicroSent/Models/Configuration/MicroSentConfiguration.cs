namespace MicroSent.Models.Configuration
{
    public class MicroSentConfiguration : IAlgorithmConfiguration
    {
        public bool testing { get; set; }
        public bool useGoogleParser { get; set; }
        public bool useSerializedData { get; set; }
        public bool serializeData { get; set; }


        public bool intensifyLastSentence { get; set; }
        public int skipTweetsAmount { get; set; }


        public int minimalEmojiOccurences { get; set; }
        public float minimalPositiveEmojiScore { get; set; }
        public float minimalNegativeEmojiScore { get; set; }

        public int clientSendingPort { get; set; }
        public int clientReceivingPort { get; set; }
        public string clientHost { get; set; }

        public MicroSentConfiguration()
        {
            testing = true;
            useGoogleParser = true;
            useSerializedData = false;
            serializeData = true;
            skipTweetsAmount = 0;

            intensifyLastSentence = false;

            minimalEmojiOccurences = 100;
            minimalPositiveEmojiScore = .5f;
            minimalNegativeEmojiScore = .4f;

            clientSendingPort = 6048;
            clientReceivingPort = 6050;
            clientHost = "localhost";
        }
    }
}
