using System.Collections.Generic;
using System.Diagnostics;
using DungeonGen.Core;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DungeonGen.Tests
{
    /// <summary>Phase 1 acceptance: 1,000 seeds x 15 rooms, no overlaps, fully connected, deterministic.</summary>
    public class LayoutStressTests
    {
        const int Seeds = 1000;

        TestRecipes _recipes;

        [SetUp] public void SetUp() => _recipes = new TestRecipes();
        [TearDown] public void TearDown() => _recipes.Destroy();

        [Test]
        public void ThousandSeeds_FifteenRooms_ValidAndDeterministic()
        {
            DungeonRecipe recipe = _recipes.Create(mainPath: 9, branches: 2, branchLength: 3, loops: 2);
            int failures = 0, totalAttempts = 0, loopsRequested = 0, loopsBuilt = 0;
            var timer = Stopwatch.StartNew();

            for (int seed = 0; seed < Seeds; seed++)
            {
                DungeonResult r = DungeonGenerator.Generate(recipe, seed);
                if (!r.success)
                {
                    failures++;
                    Debug.Log($"Seed {seed} failed: {r.failureReason}");
                    continue;
                }

                totalAttempts += r.attempts;
                Assert.AreEqual(15, r.rooms.Count);
                AssertNoOverlaps(r, recipe.roomSpacing, seed);
                AssertCorridorsOnlyOpenAtSockets(r, seed);
                AssertConnected(r, seed);

                foreach (FlowEdge e in r.flow.edges) if (e.isLoop) loopsRequested++;
                foreach (Corridor c in r.corridors) if (c.isLoop) loopsBuilt++;
            }
            timer.Stop();

            Debug.Log($"[DungeonGen] {Seeds} seeds: {Seeds - failures} ok, {failures} failed, " +
                      $"avg attempts {(float)totalAttempts / Mathf.Max(1, Seeds - failures):0.00}, " +
                      $"loops {loopsBuilt}/{loopsRequested}, avg {timer.Elapsed.TotalMilliseconds / Seeds:0.00} ms");
            Assert.AreEqual(0, failures, "Every seed should succeed within maxAttempts.");
        }

        [Test]
        public void SameSeed_GivesIdenticalDungeon()
        {
            DungeonRecipe recipe = _recipes.Create();
            for (int seed = 0; seed < 100; seed++)
            {
                string a = TestRecipes.Fingerprint(DungeonGenerator.Generate(recipe, seed));
                string b = TestRecipes.Fingerprint(DungeonGenerator.Generate(recipe, seed));
                Assert.AreEqual(a, b, $"seed {seed} not deterministic");
            }
        }

        [Test]
        public void DifferentSeeds_GiveDifferentDungeons()
        {
            DungeonRecipe recipe = _recipes.Create();
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 50; seed++)
                seen.Add(TestRecipes.Fingerprint(DungeonGenerator.Generate(recipe, seed)));
            Assert.Greater(seen.Count, 45);
        }

        [Test]
        public void MissingTemplates_FailsWithReadableReason()
        {
            DungeonRecipe recipe = _recipes.Create();
            recipe.defaultFillTag = "boss";
            DungeonResult r = DungeonGenerator.Generate(recipe, 1);
            Assert.IsFalse(r.success);
            StringAssert.Contains("'boss'", r.failureReason);
        }

        static void AssertNoOverlaps(DungeonResult r, int spacing, int seed)
        {
            for (int i = 0; i < r.rooms.Count; i++)
            for (int j = i + 1; j < r.rooms.Count; j++)
            {
                RectInt a = RectUtility.Expand(r.rooms[i].bounds, spacing);
                Assert.IsFalse(a.Overlaps(r.rooms[j].bounds),
                    $"seed {seed}: rooms {i} and {j} closer than spacing {spacing}");
            }

            foreach (Corridor c in r.corridors)
            foreach (Vector2Int p in c.path)
            foreach (PlacedRoom room in r.rooms)
                Assert.IsFalse(RectUtility.Contains(room.bounds, p), $"seed {seed}: corridor cuts through room {room.id} at {p}");
        }

        /// <summary>Corridor floor may only touch room floor at a connected socket.</summary>
        static void AssertCorridorsOnlyOpenAtSockets(DungeonResult r, int seed)
        {
            var doors = new HashSet<(Vector2Int exit, Vector2Int socket)>();
            foreach (PlacedRoom room in r.rooms)
            foreach (int s in room.usedSockets)
                doors.Add((room.ExitCell(s), room.SocketCell(s)));

            foreach (Corridor c in r.corridors)
            foreach (Vector2Int p in c.path)
            for (int d = 0; d < 4; d++)
            {
                Vector2Int n = p + ((Direction)d).ToVector();
                if (r.grid.GetRoomId(n) < 0 || r.grid.Get(n) != CellType.Floor) continue;
                Assert.IsTrue(doors.Contains((p, n)), $"seed {seed}: unintended opening into room {r.grid.GetRoomId(n)} at {n}");
            }
        }

        static void AssertConnected(DungeonResult r, int seed)
        {
            Assert.IsTrue(ConnectivityValidator.Validate(r.grid, r.playerStart, out string failure), $"seed {seed}: {failure}");

            int treeCorridors = 0;
            foreach (Corridor c in r.corridors) if (!c.isLoop) treeCorridors++;
            Assert.AreEqual(r.rooms.Count - 1, treeCorridors, $"seed {seed}: every tree edge needs a corridor");
        }
    }
}
