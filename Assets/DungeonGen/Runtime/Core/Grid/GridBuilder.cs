using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>Grid build stage: stamps rooms and corridors into a <see cref="DungeonGrid"/> and derives walls.</summary>
    public static class GridBuilder
    {
        public static DungeonGrid Build(List<PlacedRoom> rooms, List<Corridor> corridors)
        {
            RectInt area = rooms[0].bounds;
            foreach (PlacedRoom r in rooms) area = RectUtility.Union(area, r.bounds);
            foreach (Corridor c in corridors)
                foreach (Vector2Int p in c.path)
                    area = RectUtility.Union(area, new RectInt(p, Vector2Int.one));

            // One cell of margin so every floor cell can get a wall around it.
            var grid = new DungeonGrid(RectUtility.Expand(area, 1));

            foreach (PlacedRoom r in rooms)
            {
                Vector2Int size = r.template.size;
                for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    var local = new Vector2Int(x, y);
                    CellType type = r.template.GetCell(local);
                    if (type == CellType.Empty) continue;
                    grid.Set(r.origin + local, type);
                    grid.SetRoomId(r.origin + local, r.id);
                }
            }

            foreach (Corridor c in corridors)
                foreach (Vector2Int p in c.path)
                    if (grid.Get(p) == CellType.Empty) grid.Set(p, CellType.Floor);

            DeriveWalls(grid);
            return grid;
        }

        /// <summary>Every empty cell 8-adjacent to floor becomes wall.</summary>
        public static void DeriveWalls(DungeonGrid grid)
        {
            RectInt b = grid.Bounds;
            for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
            {
                var p = new Vector2Int(x, y);
                if (grid.Get(p) == CellType.Empty && grid.NeighbourMask8(p, CellType.Floor) != 0)
                    grid.Set(p, CellType.Wall);
            }
        }
    }
}
