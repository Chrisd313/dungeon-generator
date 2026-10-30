using System.Collections.Generic;
using DungeonGen.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGen
{
    /// <summary>Draws a <see cref="DungeonResult"/> onto Tilemaps. Grid cell (x, y) maps to tile cell (x, y, 0).</summary>
    public static class DungeonRenderer
    {
        /// <summary>Renders the dungeon, logging a warning if it can't.</summary>
        public static void Render(DungeonResult result, Tileset tileset, Tilemap floorTilemap, Tilemap wallTilemap)
        {
            if (!TryRender(result, tileset, floorTilemap, wallTilemap, out string failure))
                Debug.LogWarning($"[DungeonGen] {failure}");
        }

        /// <summary>
        /// Clears both tilemaps and draws floors (with weighted variants), doors and autotiled walls.
        /// The two tilemaps may be the same one.
        /// </summary>
        public static bool TryRender(DungeonResult result, Tileset tileset, Tilemap floorTilemap, Tilemap wallTilemap,
            out string failure)
        {
            if (result == null || !result.success) { failure = "Nothing to render: generation did not succeed."; return false; }
            if (tileset == null) { failure = "No tileset assigned."; return false; }
            if (floorTilemap == null || wallTilemap == null) { failure = "Floor and wall tilemaps must both be assigned."; return false; }
            failure = tileset.Validate();
            if (failure != null) return false;

            Clear(floorTilemap, wallTilemap);

            DungeonGrid grid = result.grid;
            RectInt b = grid.Bounds;
            int variantSeed = SeedUtility.Derive(result.seed, GenerationStage.Decoration);

            var noVariation = new HashSet<int>();
            foreach (PlacedRoom room in result.rooms)
                if (!room.template.allowFloorVariation) noVariation.Add(room.id);

            var doors = new HashSet<Vector2Int>();
            if (tileset.doorTile != null)
                foreach (PlacedRoom room in result.rooms)
                    foreach (int s in room.usedSockets)
                        doors.Add(room.SocketCell(s));

            var floorCells = new List<Vector3Int>(b.width * b.height / 2);
            var floorTiles = new List<TileBase>(floorCells.Capacity);
            var wallCells = new List<Vector3Int>(b.width * b.height / 2);
            var wallTiles = new List<TileBase>(wallCells.Capacity);

            for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
            {
                var p = new Vector2Int(x, y);
                switch (grid.Get(p))
                {
                    case CellType.Floor:
                        TileBase floor;
                        if (doors.Contains(p)) floor = tileset.doorTile;
                        else if (noVariation.Contains(grid.GetRoomId(p))) floor = tileset.PrimaryFloorTile;
                        else floor = tileset.PickFloorTile(SeedUtility.CellHash01(variantSeed, x, y));
                        floorCells.Add(new Vector3Int(x, y, 0));
                        floorTiles.Add(floor);
                        break;

                    case CellType.Wall:
                        WallRole role = WallRoleResolver.Resolve(grid.NeighbourMask8(p, CellType.Floor));
                        wallCells.Add(new Vector3Int(x, y, 0));
                        wallTiles.Add(tileset.GetWallTile(role));
                        break;
                }
            }

            floorTilemap.SetTiles(floorCells.ToArray(), floorTiles.ToArray());
            wallTilemap.SetTiles(wallCells.ToArray(), wallTiles.ToArray());
            return true;
        }

        public static void Clear(Tilemap floorTilemap, Tilemap wallTilemap)
        {
            if (floorTilemap != null) floorTilemap.ClearAllTiles();
            if (wallTilemap != null && wallTilemap != floorTilemap) wallTilemap.ClearAllTiles();
        }

        /// <summary>
        /// Makes the wall tilemap solid for 2D physics: a TilemapCollider2D merged into one CompositeCollider2D
        /// on a static Rigidbody2D. Safe to call repeatedly. Wall tiles need a collider type other than None.
        /// </summary>
        public static void SetupWallCollider(Tilemap wallTilemap)
        {
            GameObject go = wallTilemap.gameObject;

            var body = go.GetComponent<Rigidbody2D>();
            if (body == null) body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var composite = go.GetComponent<CompositeCollider2D>();
            if (composite == null) composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            var tileCollider = go.GetComponent<TilemapCollider2D>();
            if (tileCollider == null) tileCollider = go.AddComponent<TilemapCollider2D>();
#if UNITY_2023_1_OR_NEWER
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
            tileCollider.usedByComposite = true;
#endif
        }
    }
}
