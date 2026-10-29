using System.Collections.Generic;

namespace DungeonGen.Core
{
    /// <summary>Convenience helpers built on <see cref="IRandom"/>.</summary>
    public static class RandomExtensions
    {
        /// <summary>Returns a value in the inclusive range.</summary>
        public static int Range(this IRandom rng, IntRange range)
        {
            return rng.Range(range.min, range.max + 1);
        }

        /// <summary>Returns a random element of a non-empty list.</summary>
        public static T Pick<T>(this IRandom rng, IReadOnlyList<T> list)
        {
            return list[rng.Range(0, list.Count)];
        }

        /// <summary>Fisher-Yates shuffle in place.</summary>
        public static void Shuffle<T>(this IRandom rng, IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Picks an index by weight. Returns -1 if the list is empty or no weight is positive.
        /// </summary>
        public static int PickWeightedIndex(this IRandom rng, IReadOnlyList<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
                if (weights[i] > 0f) total += weights[i];
            if (total <= 0f) return -1;

            float roll = rng.NextFloat() * total;
            int last = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f) continue;
                last = i;
                roll -= weights[i];
                if (roll < 0f) return i;
            }
            return last; // float rounding fallback
        }
    }
}
