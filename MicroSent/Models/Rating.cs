using System;

namespace MicroSent.Models
{
    [Serializable]
    public class Rating
    {
        public string entityName { get; }
        public float averageRating { get; }
        public int occurences { get; }

        public Rating(string entityName, float averageRating, int occurences)
        {
            this.entityName = entityName;
            this.averageRating = averageRating;
            this.occurences = occurences;
        }
    }
}
