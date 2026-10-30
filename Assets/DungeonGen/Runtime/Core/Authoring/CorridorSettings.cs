using System;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>How corridors between rooms are routed and carved.</summary>
    [Serializable]
    public class CorridorSettings
    {
        [Min(1)] public int width = 1;

        [Tooltip("Extra cost for each change of direction. Higher values give straighter corridors.")]
        [Min(0f)] public float turnPenalty = 2f;

        [Tooltip("Longest corridor allowed, in cells. Loop corridors longer than this are dropped.")]
        [Min(1)] public int maxLength = 60;

        [Tooltip("When on, corridors prefer to reuse existing corridor cells.")]
        public bool allowMerging = true;
    }
}
