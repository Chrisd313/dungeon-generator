using System;

namespace DungeonGen.Core
{
    /// <summary>Inclusive integer range, e.g. 8 to 12.</summary>
    [Serializable]
    public struct IntRange
    {
        public int min;
        public int max;

        public IntRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Contains(int value) => value >= min && value <= max;

        public bool IsValid => max >= min;

        public override string ToString() => min == max ? min.ToString() : $"{min}-{max}";
    }
}
