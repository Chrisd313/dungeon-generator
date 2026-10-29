using DungeonGen.Core;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGen.Tests
{
    public class CorridorPathfinderTests
    {
        static CorridorSettings Settings(float turnPenalty = 2f, bool merging = true) =>
            new CorridorSettings { turnPenalty = turnPenalty, allowMerging = merging, maxLength = 1000 };

        [Test]
        public void FindsStraightPath()
        {
            var f = new CorridorPathfinder(new RectInt(0, 0, 10, 10));
            var path = f.FindPath(new Vector2Int(0, 5), Direction.East, new Vector2Int(9, 5), Settings(), 1000);
            Assert.IsNotNull(path);
            Assert.AreEqual(10, path.Count);
            Assert.AreEqual(new Vector2Int(0, 5), path[0]);
            Assert.AreEqual(new Vector2Int(9, 5), path[9]);
        }

        [Test]
        public void PathIsFourConnectedAndAvoidsBlockedCells()
        {
            var f = new CorridorPathfinder(new RectInt(0, 0, 20, 20));
            f.Block(new RectInt(5, 0, 2, 15)); // wall with a gap at the top
            var path = f.FindPath(new Vector2Int(0, 0), Direction.East, new Vector2Int(10, 0), Settings(), 1000);
            Assert.IsNotNull(path);
            for (int i = 0; i < path.Count; i++)
            {
                Assert.IsFalse(f.IsBlocked(path[i]), $"path crosses blocked cell {path[i]}");
                if (i > 0)
                {
                    Vector2Int d = path[i] - path[i - 1];
                    Assert.AreEqual(1, Mathf.Abs(d.x) + Mathf.Abs(d.y));
                }
            }
        }

        [Test]
        public void TurnPenaltyPrefersFewerTurns()
        {
            var f = new CorridorPathfinder(new RectInt(0, 0, 12, 12));
            var path = f.FindPath(new Vector2Int(0, 0), Direction.East, new Vector2Int(8, 8), Settings(turnPenalty: 5f), 1000);
            Assert.IsNotNull(path);
            Assert.AreEqual(1, CountTurns(path), "An L-shape is the cheapest route with a high turn penalty.");
        }

        [Test]
        public void ReturnsNullWhenUnreachable()
        {
            var f = new CorridorPathfinder(new RectInt(0, 0, 10, 10));
            f.Block(new RectInt(5, 0, 1, 10));
            Assert.IsNull(f.FindPath(new Vector2Int(0, 0), Direction.East, new Vector2Int(9, 0), Settings(), 1000));
        }

        [Test]
        public void RespectsMaxLength()
        {
            var f = new CorridorPathfinder(new RectInt(0, 0, 20, 3));
            Assert.IsNull(f.FindPath(new Vector2Int(0, 1), Direction.East, new Vector2Int(19, 1), Settings(), 10));
            Assert.IsNotNull(f.FindPath(new Vector2Int(0, 1), Direction.East, new Vector2Int(19, 1), Settings(), 20));
        }

        [Test]
        public void MergingPrefersExistingCorridor()
        {
            var f = new CorridorPathfinder(new RectInt(0, 0, 12, 12));
            // Existing corridor along y = 6.
            var existing = f.FindPath(new Vector2Int(0, 6), Direction.East, new Vector2Int(11, 6), Settings(), 1000);
            f.MarkCorridor(existing);

            var path = f.FindPath(new Vector2Int(0, 4), Direction.East, new Vector2Int(11, 4),
                Settings(turnPenalty: 0f, merging: true), 1000);
            int onExisting = 0;
            foreach (Vector2Int p in path) if (f.IsCorridor(p)) onExisting++;
            Assert.Greater(onExisting, 5, "With merging on and no turn penalty, the cheaper corridor cells win.");
        }

        static int CountTurns(System.Collections.Generic.List<Vector2Int> path)
        {
            int turns = 0;
            for (int i = 2; i < path.Count; i++)
                if (path[i] - path[i - 1] != path[i - 1] - path[i - 2]) turns++;
            return turns;
        }
    }
}
