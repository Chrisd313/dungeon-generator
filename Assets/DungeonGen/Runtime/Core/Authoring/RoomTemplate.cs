using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>
    /// A hand-authored room: a small grid of cells plus door sockets, spawn zones and selection rules.
    /// </summary>
    [CreateAssetMenu(menuName = "DungeonGen/Room Template", fileName = "RoomTemplate")]
    public class RoomTemplate : ScriptableObject
    {
        public Vector2Int size = new Vector2Int(7, 7);

        /// <summary>Flattened cells, index = y * size.x + x.</summary>
        public CellType[] cells = new CellType[49];

        public List<DoorSocket> sockets = new List<DoorSocket>();
        public List<SpawnZone> spawnZones = new List<SpawnZone>();
        public List<string> tags = new List<string>();

        [Min(0f)] public float weight = 1f;

        [Tooltip("Smallest flow depth this room may appear at. -1 = no limit.")]
        public int minDepth = -1;

        [Tooltip("Largest flow depth this room may appear at. -1 = no limit.")]
        public int maxDepth = -1;

        public bool allowFloorVariation = true;

        public bool InBounds(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < size.x && p.y < size.y;

        /// <summary>Cell at a template-local position; out of bounds reads as Empty.</summary>
        public CellType GetCell(Vector2Int p)
        {
            if (!InBounds(p) || cells == null) return CellType.Empty;
            int i = p.y * size.x + p.x;
            return i < cells.Length ? cells[i] : CellType.Empty;
        }

        public void SetCell(Vector2Int p, CellType type)
        {
            if (InBounds(p)) cells[p.y * size.x + p.x] = type;
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return true;
            for (int i = 0; i < tags.Count; i++)
                if (tags[i] == tag) return true;
            return false;
        }

        public bool AllowsDepth(int depth) =>
            (minDepth < 0 || depth >= minDepth) && (maxDepth < 0 || depth <= maxDepth);

        /// <summary>
        /// Creates an all-floor rectangular template with one socket centred on each side.
        /// Handy for prototyping and tests; real projects author templates in the Room Template Editor.
        /// </summary>
        public static RoomTemplate CreateRectangle(int width, int height, params string[] tags)
        {
            var t = CreateInstance<RoomTemplate>();
            t.name = $"Rect {width}x{height}";
            t.size = new Vector2Int(width, height);
            t.cells = new CellType[width * height];
            for (int i = 0; i < t.cells.Length; i++) t.cells[i] = CellType.Floor;

            t.sockets.Add(new DoorSocket(new Vector2Int(width / 2, height - 1), Direction.North));
            t.sockets.Add(new DoorSocket(new Vector2Int(width - 1, height / 2), Direction.East));
            t.sockets.Add(new DoorSocket(new Vector2Int(width / 2, 0), Direction.South));
            t.sockets.Add(new DoorSocket(new Vector2Int(0, height / 2), Direction.West));

            t.tags.AddRange(tags);
            return t;
        }
    }
}
