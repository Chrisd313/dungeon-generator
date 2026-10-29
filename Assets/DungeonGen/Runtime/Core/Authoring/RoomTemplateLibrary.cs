using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>The pool of room templates a recipe draws from.</summary>
    [CreateAssetMenu(menuName = "DungeonGen/Room Template Library", fileName = "RoomTemplateLibrary")]
    public class RoomTemplateLibrary : ScriptableObject
    {
        public List<RoomTemplate> templates = new List<RoomTemplate>();

        /// <summary>
        /// Adds every template that has <paramref name="tag"/>, allows <paramref name="depth"/>,
        /// has positive weight and at least <paramref name="minSockets"/> sockets.
        /// </summary>
        public void GetCandidates(string tag, int depth, int minSockets, List<RoomTemplate> results)
        {
            results.Clear();
            for (int i = 0; i < templates.Count; i++)
            {
                RoomTemplate t = templates[i];
                if (t == null || t.weight <= 0f) continue;
                if (!t.HasTag(tag) || !t.AllowsDepth(depth)) continue;
                if (t.sockets.Count < minSockets) continue;
                results.Add(t);
            }
        }

        /// <summary>Weighted random pick among templates matching tag and depth, or null if none match.</summary>
        public RoomTemplate Pick(string tag, int depth, IRandom rng)
        {
            var candidates = new List<RoomTemplate>();
            GetCandidates(tag, depth, 0, candidates);
            if (candidates.Count == 0) return null;

            var weights = new List<float>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++) weights.Add(candidates[i].weight);
            return candidates[rng.PickWeightedIndex(weights)];
        }
    }
}
