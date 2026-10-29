using System;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>
    /// Dense grid over a bounding rectangle. Stores a <see cref="CellType"/> and owning room id per cell
    /// (<see cref="NoRoom"/> = corridor or nothing). Coordinates are grid coordinates, not array indices.
    /// </summary>
    public sealed class DungeonGrid
    {
        public const int NoRoom = -1;

        /// <summary>8-neighbour offsets in bit order: N, NE, E, SE, S, SW, W, NW.</summary>
        public static readonly Vector2Int[] Neighbours8 =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1),
        };

        readonly CellType[] _cells;
        readonly int[] _roomIds;

        public RectInt Bounds { get; }
        public int Width => Bounds.width;
        public int Height => Bounds.height;

        public DungeonGrid(RectInt bounds)
        {
            if (bounds.width <= 0 || bounds.height <= 0)
                throw new ArgumentException($"Grid bounds must be positive, got {bounds}.");

            Bounds = bounds;
            _cells = new CellType[bounds.width * bounds.height];
            _roomIds = new int[bounds.width * bounds.height];
            for (int i = 0; i < _roomIds.Length; i++) _roomIds[i] = NoRoom;
        }

        public bool InBounds(Vector2Int p) => InBounds(p.x, p.y);

        public bool InBounds(int x, int y) =>
            x >= Bounds.xMin && x < Bounds.xMax && y >= Bounds.yMin && y < Bounds.yMax;

        int Index(int x, int y) => (y - Bounds.yMin) * Bounds.width + (x - Bounds.xMin);

        /// <summary>Cell type at a position; out-of-bounds reads as <see cref="CellType.Empty"/>.</summary>
        public CellType Get(Vector2Int p) => Get(p.x, p.y);

        public CellType Get(int x, int y) => InBounds(x, y) ? _cells[Index(x, y)] : CellType.Empty;

        public void Set(Vector2Int p, CellType type) => Set(p.x, p.y, type);

        public void Set(int x, int y, CellType type)
        {
            if (!InBounds(x, y))
                throw new ArgumentOutOfRangeException(nameof(x), $"({x}, {y}) is outside grid bounds {Bounds}.");
            _cells[Index(x, y)] = type;
        }

        /// <summary>Room id at a position; out-of-bounds reads as <see cref="NoRoom"/>.</summary>
        public int GetRoomId(Vector2Int p) => InBounds(p) ? _roomIds[Index(p.x, p.y)] : NoRoom;

        public void SetRoomId(Vector2Int p, int roomId)
        {
            if (!InBounds(p))
                throw new ArgumentOutOfRangeException(nameof(p), $"{p} is outside grid bounds {Bounds}.");
            _roomIds[Index(p.x, p.y)] = roomId;
        }

        /// <summary>
        /// Bitmask of which 8-neighbours are <paramref name="type"/>. Bit i corresponds to <see cref="Neighbours8"/>[i].
        /// </summary>
        public int NeighbourMask8(Vector2Int p, CellType type)
        {
            int mask = 0;
            for (int i = 0; i < 8; i++)
                if (Get(p + Neighbours8[i]) == type) mask |= 1 << i;
            return mask;
        }

        /// <summary>Number of cells of the given type.</summary>
        public int Count(CellType type)
        {
            int n = 0;
            for (int i = 0; i < _cells.Length; i++)
                if (_cells[i] == type) n++;
            return n;
        }
    }
}
