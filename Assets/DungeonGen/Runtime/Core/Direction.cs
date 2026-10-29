using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>Cardinal direction. North is +Y.</summary>
    public enum Direction : byte
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
    }

    public static class DirectionExtensions
    {
        static readonly Vector2Int[] Vectors =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
        };

        public static Vector2Int ToVector(this Direction d) => Vectors[(int)d];

        public static Direction Opposite(this Direction d) => (Direction)(((int)d + 2) & 3);

        /// <summary>Direction rotated 90 degrees clockwise.</summary>
        public static Direction Clockwise(this Direction d) => (Direction)(((int)d + 1) & 3);

        public static bool IsHorizontal(this Direction d) => d == Direction.East || d == Direction.West;
    }
}
