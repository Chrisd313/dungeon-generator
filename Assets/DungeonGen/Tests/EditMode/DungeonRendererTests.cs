using System.Collections.Generic;
using DungeonGen.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGen.Tests
{
    /// <summary>Phase 2 acceptance: renders with minimal and full tilesets, no missing-tile gaps.</summary>
    public class DungeonRendererTests
    {
        TestRecipes _recipes;
        GameObject _gridObject;
        Tilemap _floor, _walls;

        [SetUp]
        public void SetUp()
        {
            _recipes = new TestRecipes();
            _gridObject = new GameObject("Grid", typeof(Grid));
            _floor = NewTilemap("Floor");
            _walls = NewTilemap("Walls");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gridObject);
            _recipes.Destroy();
        }

        Tilemap NewTilemap(string name)
        {
            var go = new GameObject(name, typeof(Tilemap));
            go.transform.SetParent(_gridObject.transform);
            return go.GetComponent<Tilemap>();
        }

        Tile NewTile(string name)
        {
            var t = _recipes.Track(ScriptableObject.CreateInstance<Tile>());
            t.name = name;
            return t;
        }

        Tileset MinimalTileset()
        {
            var ts = _recipes.Track(ScriptableObject.CreateInstance<Tileset>());
            ts.name = "Minimal";
            ts.wallDefault = NewTile("wall");
            ts.floorTiles.Add(new WeightedTile(NewTile("floor")));
            return ts;
        }

        Tileset FullTileset()
        {
            Tileset ts = MinimalTileset();
            ts.name = "Full";
            ts.floorTiles.Add(new WeightedTile(NewTile("floor2"), 1f));
            ts.floorTiles.Add(new WeightedTile(NewTile("floor3"), 1f));
            ts.doorTile = NewTile("door");
            ts.wallTop = NewTile("top");
            ts.wallBottom = NewTile("bottom");
            ts.wallLeft = NewTile("left");
            ts.wallRight = NewTile("right");
            ts.outerTopLeft = NewTile("otl");
            ts.outerTopRight = NewTile("otr");
            ts.outerBottomLeft = NewTile("obl");
            ts.outerBottomRight = NewTile("obr");
            ts.innerTopLeft = NewTile("itl");
            ts.innerTopRight = NewTile("itr");
            ts.innerBottomLeft = NewTile("ibl");
            ts.innerBottomRight = NewTile("ibr");
            return ts;
        }

        [Test]
        public void MinimalTileset_LeavesNoGaps() => AssertNoGaps(MinimalTileset());

        [Test]
        public void FullTileset_LeavesNoGapsAndUsesRoleTiles() => AssertNoGaps(FullTileset());

        void AssertNoGaps(Tileset tileset)
        {
            DungeonRecipe recipe = _recipes.Create();
            for (int seed = 0; seed < 20; seed++)
            {
                DungeonResult r = DungeonGenerator.Generate(recipe, seed);
                Assert.IsTrue(r.success, r.failureReason);
                Assert.IsTrue(DungeonRenderer.TryRender(r, tileset, _floor, _walls, out string failure), failure);

                RectInt b = r.grid.Bounds;
                for (int y = b.yMin - 1; y <= b.yMax; y++)
                for (int x = b.xMin - 1; x <= b.xMax; x++)
                {
                    var p = new Vector2Int(x, y);
                    var cell = new Vector3Int(x, y, 0);
                    TileBase floor = _floor.GetTile(cell), wall = _walls.GetTile(cell);
                    switch (r.grid.Get(p))
                    {
                        case CellType.Floor:
                            Assert.IsNotNull(floor, $"seed {seed}: missing floor tile at {p}");
                            Assert.IsNull(wall);
                            break;
                        case CellType.Wall:
                            WallRole role = WallRoleResolver.Resolve(r.grid.NeighbourMask8(p, CellType.Floor));
                            Assert.AreSame(tileset.GetWallTile(role), wall, $"seed {seed}: wrong wall tile at {p}");
                            Assert.IsNull(floor);
                            break;
                        default:
                            Assert.IsNull(floor, $"seed {seed}: stray floor tile at {p}");
                            Assert.IsNull(wall, $"seed {seed}: stray wall tile at {p}");
                            break;
                    }
                }
            }
        }

        [Test]
        public void RoomsAreFullyEnclosedByWalls()
        {
            DungeonResult r = DungeonGenerator.Generate(_recipes.Create(), 3);
            RectInt b = r.grid.Bounds;
            for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
            {
                var p = new Vector2Int(x, y);
                if (r.grid.Get(p) != CellType.Floor) continue;
                foreach (Vector2Int d in DungeonGrid.Neighbours8)
                    Assert.AreNotEqual(CellType.Empty, r.grid.Get(p + d), $"floor at {p} touches the void at {p + d}");
            }
        }

        [Test]
        public void FloorVariantsAreDeterministicAndVaried()
        {
            Tileset tileset = FullTileset();
            DungeonResult r = DungeonGenerator.Generate(_recipes.Create(), 7);

            DungeonRenderer.TryRender(r, tileset, _floor, _walls, out _);
            var first = Snapshot(_floor, r.grid.Bounds);
            DungeonRenderer.TryRender(r, tileset, _floor, _walls, out _);
            CollectionAssert.AreEqual(first, Snapshot(_floor, r.grid.Bounds));

            var distinct = new HashSet<TileBase>(first);
            distinct.Remove(null);
            distinct.Remove(tileset.doorTile);
            Assert.AreEqual(3, distinct.Count, "All three floor variants should appear.");
        }

        [Test]
        public void DoorTileGoesOnConnectedSockets()
        {
            Tileset tileset = FullTileset();
            DungeonResult r = DungeonGenerator.Generate(_recipes.Create(), 2);
            DungeonRenderer.TryRender(r, tileset, _floor, _walls, out _);
            foreach (PlacedRoom room in r.rooms)
                foreach (int s in room.usedSockets)
                {
                    Vector2Int c = room.SocketCell(s);
                    Assert.AreSame(tileset.doorTile, _floor.GetTile(new Vector3Int(c.x, c.y, 0)));
                }
        }

        [Test]
        public void InvalidTileset_FailsWithReason()
        {
            var empty = _recipes.Track(ScriptableObject.CreateInstance<Tileset>());
            DungeonResult r = DungeonGenerator.Generate(_recipes.Create(), 1);
            Assert.IsFalse(DungeonRenderer.TryRender(r, empty, _floor, _walls, out string failure));
            StringAssert.Contains("Default wall", failure);
        }

        [Test]
        public void SetupWallCollider_AddsMergedStaticCollider()
        {
            DungeonRenderer.SetupWallCollider(_walls);
            DungeonRenderer.SetupWallCollider(_walls); // idempotent

            Assert.AreEqual(1, _walls.GetComponents<TilemapCollider2D>().Length);
            Assert.AreEqual(RigidbodyType2D.Static, _walls.GetComponent<Rigidbody2D>().bodyType);
            Assert.IsNotNull(_walls.GetComponent<CompositeCollider2D>());
#if UNITY_2023_1_OR_NEWER
            Assert.AreEqual(Collider2D.CompositeOperation.Merge, _walls.GetComponent<TilemapCollider2D>().compositeOperation);
#endif
        }

        static List<TileBase> Snapshot(Tilemap map, RectInt b)
        {
            var tiles = new List<TileBase>();
            for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
                tiles.Add(map.GetTile(new Vector3Int(x, y, 0)));
            return tiles;
        }
    }
}
