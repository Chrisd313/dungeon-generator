using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>A set of template cells where things may spawn.</summary>
    [Serializable]
    public class SpawnZone
    {
        public string id;
        public List<Vector2Int> cells = new List<Vector2Int>();
        public string spawnTableId;
        public bool isPlayerStart;
    }
}
