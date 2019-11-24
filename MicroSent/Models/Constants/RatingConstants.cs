namespace MicroSent.Models.Constants
{
    public class RatingConstants
    {
        public const float NEUTRAL = 1f;
        public const float WORD_NEUTRAL = 0f;

        public const float POSITIVE_EMOJI = 2f;
        public const float NEGATIVE_EMOJI = -2f;
        public const float POSITIVE_SMILEY = 2f;
        public const float NEGATIVE_SMILEY = -2f;

        public const float END_HASHTAG_MULIPLIER = 2f;

        public const float REPEATED_LETTER_MULTIPLIER = 2f;
        public const float UPPERCASE_MULTIPLIER = 2f;

        public const float NEGATION = -1f;

        public const float LAST_SENTENCE_INTENSIFIER = 1.5f;
    }
}
