using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>Validation stage: every floor cell must be reachable (4-connected) from the player start.</summary>
    public static class ConnectivityValidator
    {
        public static bool Validate(DungeonGrid grid, Vector2Int start, out string failure)
        {
            if (grid.Get(start) != CellType.Floor)
            {
                failure = $"Player start {start} is not a floor cell.";
                return false;
            }

            RectInt b = grid.Bounds;
            var reached = new bool[b.width * b.height];
            var stack = new Stack<Vector2Int>();
            stack.Push(start);
            reached[Index(b, start)] = true;

            while (stack.Count > 0)
            {
                Vector2Int p = stack.Pop();
                for (int d = 0; d < 4; d++)
                {
                    Vector2Int n = p + ((Direction)d).ToVector();
                    if (grid.Get(n) != CellType.Floor) continue;
                    int i = Index(b, n);
                    if (reached[i]) continue;
                    reached[i] = true;
                    stack.Push(n);
                }
            }

            for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
            {
                var p = new Vector2Int(x, y);
                if (grid.Get(p) != CellType.Floor || reached[Index(b, p)]) continue;
                int roomId = grid.GetRoomId(p);
                failure = roomId >= 0
                    ? $"Room {roomId} has floor at {p} that can't be reached from the start."
                    : $"Corridor cell {p} can't be reached from the start.";
                return false;
            }

            failure = null;
            return true;
        }

        static int Index(RectInt b, Vector2Int p) => (p.y - b.yMin) * b.width + (p.x - b.xMin);
    }
}
