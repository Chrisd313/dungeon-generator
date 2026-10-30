using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>Everything a generation run produced. Check <see cref="success"/> before using the rest.</summary>
    public sealed class DungeonResult
    {
        public bool success;
        /// <summary>Why generation failed, phrased so a designer can act on it. Null on success.</summary>
        public string failureReason;
        public int seed;
        /// <summary>Layout attempts used, starting at 1.</summary>
        public int attempts;

        public FlowGraph flow;
        public List<PlacedRoom> rooms = new List<PlacedRoom>();
        public List<Corridor> corridors = new List<Corridor>();
        public DungeonGrid grid;
        public Vector2Int playerStart;
        public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

        public static DungeonResult Fail(int seed, string reason, int attempts = 0) =>
            new DungeonResult { success = false, seed = seed, failureReason = reason, attempts = attempts };
    }
}
