using UnityEngine;

namespace DungeonGen.Core
{
    public static class RectUtility
    {
        /// <summary>Grows a rect by <paramref name="amount"/> cells on every side.</summary>
        public static RectInt Expand(RectInt r, int amount) =>
            new RectInt(r.xMin - amount, r.yMin - amount, r.width + amount * 2, r.height + amount * 2);

        /// <summary>Smallest rect containing both inclusive cell positions.</summary>
        public static RectInt FromCells(Vector2Int a, Vector2Int b)
        {
            int xMin = Mathf.Min(a.x, b.x), yMin = Mathf.Min(a.y, b.y);
            int xMax = Mathf.Max(a.x, b.x), yMax = Mathf.Max(a.y, b.y);
            return new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }

        /// <summary>Smallest rect containing both rects.</summary>
        public static RectInt Union(RectInt a, RectInt b)
        {
            int xMin = Mathf.Min(a.xMin, b.xMin), yMin = Mathf.Min(a.yMin, b.yMin);
            int xMax = Mathf.Max(a.xMax, b.xMax), yMax = Mathf.Max(a.yMax, b.yMax);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        public static bool Contains(RectInt r, Vector2Int p) =>
            p.x >= r.xMin && p.x < r.xMax && p.y >= r.yMin && p.y < r.yMax;
    }
}
