using System.Collections.Generic;
using DungeonGen.Core;
using NUnit.Framework;

namespace DungeonGen.Tests
{
    public class RandomTests
    {
        [Test]
        public void Pcg32_MatchesReferenceImplementation()
        {
            // Reference values from pcg32-demo (pcg32_srandom_r(&rng, 42u, 54u)).
            var rng = new Pcg32(42UL, 54UL);
            uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
            foreach (uint e in expected)
                Assert.AreEqual(e, rng.NextUInt());
        }

        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new Pcg32(12345);
            var b = new Pcg32(12345);
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var a = new Pcg32(1);
            var b = new Pcg32(2);
            int same = 0;
            for (int i = 0; i < 100; i++)
                if (a.NextUInt() == b.NextUInt()) same++;
            Assert.Less(same, 3);
        }

        [Test]
        public void Range_StaysWithinBoundsAndHitsEveryValue()
        {
            var rng = new Pcg32(7);
            var seen = new HashSet<int>();
            for (int i = 0; i < 10000; i++)
            {
                int v = rng.Range(-3, 4);
                Assert.That(v, Is.InRange(-3, 3));
                seen.Add(v);
            }
            Assert.AreEqual(7, seen.Count);
        }

        [Test]
        public void Range_EmptyRangeReturnsMin()
        {
            Assert.AreEqual(5, new Pcg32(0).Range(5, 5));
        }

        [Test]
        public void NextFloat_IsInUnitInterval()
        {
            var rng = new Pcg32(99);
            for (int i = 0; i < 10000; i++)
            {
                float f = rng.NextFloat();
                Assert.GreaterOrEqual(f, 0f);
                Assert.Less(f, 1f);
            }
        }

        [Test]
        public void IntRange_IsInclusive()
        {
            var rng = new Pcg32(3);
            var seen = new HashSet<int>();
            for (int i = 0; i < 1000; i++) seen.Add(rng.Range(new IntRange(2, 4)));
            CollectionAssert.AreEquivalent(new[] { 2, 3, 4 }, seen);
        }

        [Test]
        public void PickWeightedIndex_NeverPicksZeroWeight()
        {
            var rng = new Pcg32(11);
            var weights = new List<float> { 0f, 1f, 0f, 3f };
            int ones = 0, threes = 0;
            for (int i = 0; i < 4000; i++)
            {
                int idx = rng.PickWeightedIndex(weights);
                Assert.That(idx == 1 || idx == 3);
                if (idx == 1) ones++; else threes++;
            }
            Assert.That(threes, Is.GreaterThan(ones * 2));
        }

        [Test]
        public void PickWeightedIndex_AllZeroReturnsMinusOne()
        {
            Assert.AreEqual(-1, new Pcg32(1).PickWeightedIndex(new List<float> { 0f, 0f }));
        }

        [Test]
        public void SubSeeds_AreDeterministicAndIndependentPerStage()
        {
            Assert.AreEqual(
                SeedUtility.Derive(42, GenerationStage.Layout),
                SeedUtility.Derive(42, GenerationStage.Layout));

            var seeds = new HashSet<int>();
            foreach (GenerationStage stage in System.Enum.GetValues(typeof(GenerationStage)))
                seeds.Add(SeedUtility.Derive(42, stage));
            Assert.AreEqual(System.Enum.GetValues(typeof(GenerationStage)).Length, seeds.Count);

            Assert.AreNotEqual(
                SeedUtility.Derive(42, GenerationStage.Layout, 0),
                SeedUtility.Derive(42, GenerationStage.Layout, 1));
            Assert.AreNotEqual(
                SeedUtility.Derive(42, GenerationStage.Layout),
                SeedUtility.Derive(43, GenerationStage.Layout));
        }
    }
}
