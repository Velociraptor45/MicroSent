﻿namespace MicroSent.Models.Configuration
{
    public interface IAlgorithmConfiguration
    {
        bool testing { get; }
        bool useGoogleParser { get; }
        bool useSerializedData { get; }
        bool serializeData { get; }

        bool intensifyLastSentence { get; }
        int skipTweetsAmount { get; }
        
        //emojis:
        int minimalEmojiOccurences { get; }
        float minimalPositiveEmojiScore { get; }
        float minimalNegativeEmojiScore { get; }

        //network:
        int clientSendingPort { get; }
        int clientReceivingPort { get; }
        string clientHost { get; }

        //rating:
        bool useOnlyAverageRatingScore { get; }
        bool useSingleTokenThreshold { get; }
        bool useTotalThreshold { get; }
        float singleTokenThreshold { get; }
        float totalThreshold { get; }

        //negation:
        int negationWindowSize { get; }
    }
}
