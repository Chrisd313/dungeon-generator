namespace DungeonGen.Core
{
    /// <summary>
    /// Deterministic random source used by every generation stage.
    /// Never use <c>UnityEngine.Random</c> in generation code; take an <see cref="IRandom"/> instead.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Returns a uniformly distributed 32-bit unsigned integer.</summary>
        uint NextUInt();

        /// <summary>Returns an integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>Returns a float in [0, 1).</summary>
        float NextFloat();

        /// <summary>Returns true with the given probability (0..1).</summary>
        bool Chance(float probability);
    }
}
