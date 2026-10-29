using System;

namespace DungeonGen.Core
{
    /// <summary>
    /// PCG32 (XSH-RR) pseudo-random generator. Small, fast, and identical on every platform,
    /// which is what makes "same recipe + seed = same dungeon" possible.
    /// </summary>
    public sealed class Pcg32 : IRandom
    {
        const ulong Multiplier = 6364136223846793005UL;
        const ulong DefaultStream = 0xda3e39cb94b95bdbUL;

        ulong _state;
        readonly ulong _increment;

        public Pcg32(int seed) : this((ulong)(uint)seed, DefaultStream) { }

        public Pcg32(ulong seed, ulong stream)
        {
            _increment = (stream << 1) | 1UL;
            _state = 0;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + _increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                if (maxExclusive == minInclusive) return minInclusive;
                throw new ArgumentException($"Range max ({maxExclusive}) must be >= min ({minInclusive}).");
            }

            uint bound = (uint)((long)maxExclusive - minInclusive);
            // Rejection sampling removes modulo bias.
            uint threshold = unchecked((uint)-bound) % bound;
            uint r;
            do { r = NextUInt(); } while (r < threshold);
            return (int)(minInclusive + (long)(r % bound));
        }

        public float NextFloat()
        {
            // 24 random bits -> exactly representable float in [0, 1).
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public bool Chance(float probability)
        {
            return NextFloat() < probability;
        }
    }
}
