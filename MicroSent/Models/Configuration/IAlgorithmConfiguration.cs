namespace MicroSent.Models.Configuration
{
    public interface IAlgorithmConfiguration
    {
        bool testing { get; set; }
        bool useGoogleParser { get; set; }
        bool useSerializedData { get; set; }
        bool serializeData { get; set; }

        bool intensifyLastSentence { get; set; }
        int skipTweetsAmount { get; set; }
        
        //emojis:
        int minimalEmojiOccurences { get; set; }
        float minimalPositiveEmojiScore { get; set; }
        float minimalNegativeEmojiScore { get; set; }

        //network:
        int clientSendingPort { get; set; }
        int clientReceivingPort { get; set; }
        string clientHost { get; set; }
    }
}
