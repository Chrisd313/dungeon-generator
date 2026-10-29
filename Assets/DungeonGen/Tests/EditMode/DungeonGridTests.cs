using DungeonGen.Core;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGen.Tests
{
    public class DungeonGridTests
    {
        [Test]
        public void NewGrid_IsEmptyWithNoRooms()
        {
            var grid = new DungeonGrid(new RectInt(-5, -5, 10, 10));
            Assert.AreEqual(100, grid.Count(CellType.Empty));
            Assert.AreEqual(DungeonGrid.NoRoom, grid.GetRoomId(new Vector2Int(0, 0)));
        }

        [Test]
        public void SetAndGet_UseGridCoordinatesWithNegativeOrigin()
        {
            var grid = new DungeonGrid(new RectInt(-5, -5, 10, 10));
            grid.Set(new Vector2Int(-5, -5), CellType.Floor);
            grid.Set(new Vector2Int(4, 4), CellType.Wall);
            grid.SetRoomId(new Vector2Int(-5, -5), 3);

            Assert.AreEqual(CellType.Floor, grid.Get(-5, -5));
            Assert.AreEqual(CellType.Wall, grid.Get(4, 4));
            Assert.AreEqual(3, grid.GetRoomId(new Vector2Int(-5, -5)));
        }

        [Test]
        public void OutOfBounds_ReadsEmptyAndWriteThrows()
        {
            var grid = new DungeonGrid(new RectInt(0, 0, 3, 3));
            Assert.IsFalse(grid.InBounds(3, 0));
            Assert.AreEqual(CellType.Empty, grid.Get(3, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => grid.Set(3, 0, CellType.Floor));
        }

        [Test]
        public void NeighbourMask8_SetsBitsInDocumentedOrder()
        {
            var grid = new DungeonGrid(new RectInt(0, 0, 3, 3));
            grid.Set(1, 2, CellType.Floor); // N  -> bit 0
            grid.Set(2, 1, CellType.Floor); // E  -> bit 2
            grid.Set(0, 0, CellType.Floor); // SW -> bit 5

            int mask = grid.NeighbourMask8(new Vector2Int(1, 1), CellType.Floor);
            Assert.AreEqual((1 << 0) | (1 << 2) | (1 << 5), mask);
        }

        [Test]
        public void Direction_OppositeAndVectors()
        {
            Assert.AreEqual(Direction.South, Direction.North.Opposite());
            Assert.AreEqual(Direction.West, Direction.East.Opposite());
            Assert.AreEqual(Vector2Int.up, Direction.North.ToVector());
            Assert.AreEqual(Vector2Int.left, Direction.West.ToVector());
        }
    }
}
