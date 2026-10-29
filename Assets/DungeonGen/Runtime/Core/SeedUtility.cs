namespace DungeonGen.Core
{
    /// <summary>Pipeline stages that each receive their own derived sub-seed.</summary>
    public enum GenerationStage
    {
        Flow = 1,
        Assignment = 2,
        Layout = 3,
        Corridors = 4,
        Decoration = 5,
        Spawns = 6,
    }

    /// <summary>
    /// Derives independent sub-seeds from a master seed so that changing how one stage
    /// consumes random numbers never reshuffles another stage.
    /// </summary>
    public static class SeedUtility
    {
        /// <summary>Sub-seed for a stage on a given retry attempt.</summary>
        public static int Derive(int masterSeed, GenerationStage stage, int attempt = 0)
        {
            ulong x = (uint)masterSeed;
            x = Mix(x ^ ((ulong)(uint)stage << 32));
            x = Mix(x ^ (uint)attempt);
            return (int)(x ^ (x >> 32));
        }

        /// <summary>Creates the random source for a stage.</summary>
        public static IRandom CreateRandom(int masterSeed, GenerationStage stage, int attempt = 0)
        {
            return new Pcg32(Derive(masterSeed, stage, attempt));
        }

        // SplitMix64 finalizer.
        static ulong Mix(ulong z)
        {
            unchecked
            {
                z += 0x9e3779b97f4a7c15UL;
                z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL;
                z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL;
                return z ^ (z >> 31);
            }
        }
    }
}
