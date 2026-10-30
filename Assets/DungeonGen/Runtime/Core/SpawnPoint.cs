using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>A place where the user's code should spawn something identified by <see cref="key"/>.</summary>
    public struct SpawnPoint
    {
        public string key;
        public Vector2Int cell;
        public int roomId;
        public string zoneId;
    }
}
