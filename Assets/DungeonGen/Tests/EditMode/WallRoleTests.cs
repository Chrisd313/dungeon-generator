using DungeonGen.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGen.Tests
{
    public class WallRoleTests
    {
        // Neighbour bits: N, NE, E, SE, S, SW, W, NW.
        const int N = 1, NE = 2, E = 4, SE = 8, S = 16, SW = 32, W = 64, NW = 128;

        [TestCase(S, WallRole.Top)]
        [TestCase(S | SE | SW, WallRole.Top)]
        [TestCase(N, WallRole.Bottom)]
        [TestCase(E, WallRole.Left)]
        [TestCase(W, WallRole.Right)]
        [TestCase(SE, WallRole.OuterTopLeft)]
        [TestCase(SW, WallRole.OuterTopRight)]
        [TestCase(NE, WallRole.OuterBottomLeft)]
        [TestCase(NW, WallRole.OuterBottomRight)]
        [TestCase(S | E | SE, WallRole.InnerTopLeft)]
        [TestCase(S | W, WallRole.InnerTopRight)]
        [TestCase(N | E, WallRole.InnerBottomLeft)]
        [TestCase(N | W | NW, WallRole.InnerBottomRight)]
        [TestCase(N | S, WallRole.Default)]
        [TestCase(E | W, WallRole.Default)]
        [TestCase(N | E | S, WallRole.Default)]
        [TestCase(NE | SW, WallRole.Default)]
        [TestCase(0, WallRole.Default)]
        public void Resolve_MapsFloorNeighboursToRole(int mask, WallRole expected)
        {
            Assert.AreEqual(expected, WallRoleResolver.Resolve(mask));
        }

        [Test]
        public void EveryFallbackChainEndsAtDefault()
        {
            for (int r = 0; r < WallRoleResolver.RoleCount; r++)
            {
                var role = (WallRole)r;
                for (int steps = 0; role != WallRole.Default; steps++)
                {
                    Assert.Less(steps, 5, $"fallback chain from {(WallRole)r} doesn't terminate");
                    role = WallRoleResolver.Fallback(role);
                }
            }
        }

        [Test]
        public void Tileset_FallsBackThroughChain()
        {
            var tileset = ScriptableObject.CreateInstance<Tileset>();
            var def = ScriptableObject.CreateInstance<Tile>();
            var top = ScriptableObject.CreateInstance<Tile>();
            try
            {
                tileset.wallDefault = def;
                Assert.AreSame(def, tileset.GetWallTile(WallRole.InnerBottomLeft));

                tileset.wallTop = top;
                Assert.AreSame(top, tileset.GetWallTile(WallRole.Bottom), "Bottom falls back to Top");
                Assert.AreSame(top, tileset.GetWallTile(WallRole.OuterBottomRight), "Outer bottom -> Bottom -> Top");
                Assert.AreSame(def, tileset.GetWallTile(WallRole.Right), "Right -> Left -> Default");
                Assert.AreEqual(WallRole.Top, tileset.GetEffectiveRole(WallRole.InnerTopLeft));
            }
            finally
            {
                Object.DestroyImmediate(tileset);
                Object.DestroyImmediate(def);
                Object.DestroyImmediate(top);
            }
        }
    }
}
