using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>
    /// Everything needed to generate a dungeon apart from the seed.
    /// </summary>
    [CreateAssetMenu(menuName = "DungeonGen/Dungeon Recipe", fileName = "DungeonRecipe")]
    public class DungeonRecipe : ScriptableObject
    {
        [Header("Flow")]
        [Tooltip("Rooms on the main path, including start and end.")]
        public IntRange mainPathLength = new IntRange(8, 12);
        public IntRange branchCount = new IntRange(1, 3);
        public IntRange branchLength = new IntRange(1, 3);
        [Tooltip("Extra connections between rooms of similar depth. Best-effort: unroutable loops are dropped.")]
        public IntRange loopCount = new IntRange(0, 2);
        [Tooltip("Tag given to rooms no rule claims.")]
        public string defaultFillTag = "combat";

        [Header("Content")]
        public RoomTemplateLibrary templates;
        public Tileset tileset;
        public CorridorSettings corridors = new CorridorSettings();

        [Header("Layout")]
        [Tooltip("Minimum empty cells between rooms. Leaves space for corridors.")]
        [Min(1)] public int roomSpacing = 3;
        [Tooltip("Whole-dungeon layout retries before generation reports failure.")]
        [Min(1)] public int maxAttempts = 20;
    }
}
