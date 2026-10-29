using System.Collections.Generic;
using DungeonGen.Core;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// Development aid: generates a dungeon and draws it with Gizmos in the Scene view.
    /// With no recipe assigned it uses a built-in set of rectangle rooms.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("DungeonGen/Dungeon Debug View")]
    public class DungeonDebugView : MonoBehaviour
    {
        [Tooltip("Leave empty to use built-in rectangle rooms.")]
        public DungeonRecipe recipe;
        public int seed;
        public bool drawFlowGraph = true;

        DungeonResult _result;
        DungeonRecipe _fallbackRecipe;
        readonly List<Object> _fallbackAssets = new List<Object>();

        public DungeonResult Result => _result;

        [ContextMenu("Generate")]
        public void Generate()
        {
            DungeonRecipe r = recipe != null ? recipe : GetFallbackRecipe();
            _result = DungeonGenerator.Generate(r, seed);
            if (!_result.success)
                Debug.LogWarning($"[DungeonGen] Seed {seed}: {_result.failureReason}", this);
        }

        [ContextMenu("Next Seed")]
        public void NextSeed()
        {
            seed++;
            Generate();
        }

        void OnEnable() => Generate();

        void OnValidate()
        {
            if (isActiveAndEnabled) Generate();
        }

        void OnDisable()
        {
            foreach (Object o in _fallbackAssets)
                if (o != null) DestroyImmediate(o);
            _fallbackAssets.Clear();
            _fallbackRecipe = null;
        }

        DungeonRecipe GetFallbackRecipe()
        {
            if (_fallbackRecipe != null) return _fallbackRecipe;

            var library = Hidden(ScriptableObject.CreateInstance<RoomTemplateLibrary>());
            int[,] sizes = { { 5, 5 }, { 7, 5 }, { 5, 7 }, { 7, 7 }, { 9, 7 }, { 7, 9 }, { 11, 9 } };
            for (int i = 0; i < sizes.GetLength(0); i++)
                library.templates.Add(Hidden(RoomTemplate.CreateRectangle(sizes[i, 0], sizes[i, 1], "combat")));

            _fallbackRecipe = Hidden(ScriptableObject.CreateInstance<DungeonRecipe>());
            _fallbackRecipe.templates = library;
            _fallbackRecipe.mainPathLength = new IntRange(9, 9);
            _fallbackRecipe.branchCount = new IntRange(2, 2);
            _fallbackRecipe.branchLength = new IntRange(3, 3);
            _fallbackRecipe.loopCount = new IntRange(2, 2);
            return _fallbackRecipe;
        }

        T Hidden<T>(T obj) where T : Object
        {
            obj.hideFlags = HideFlags.HideAndDontSave;
            _fallbackAssets.Add(obj);
            return obj;
        }

        void OnDrawGizmos()
        {
            if (_result == null || !_result.success) return;

            DungeonGrid grid = _result.grid;
            Vector3 cellSize = new Vector3(1f, 1f, 0f);
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            RectInt b = grid.Bounds;
            for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
            {
                var p = new Vector2Int(x, y);
                CellType type = grid.Get(p);
                if (type == CellType.Empty) continue;

                if (type == CellType.Wall) Gizmos.color = new Color(0.15f, 0.15f, 0.18f);
                else
                {
                    int roomId = grid.GetRoomId(p);
                    Gizmos.color = roomId < 0
                        ? new Color(0.75f, 0.7f, 0.6f)
                        : Color.HSVToRGB(roomId * 0.618034f % 1f, 0.45f, 0.85f);
                }
                Gizmos.DrawCube(CellCentre(p), cellSize * 0.95f);
            }

            Gizmos.color = Color.white;
            foreach (PlacedRoom room in _result.rooms)
                foreach (int s in room.usedSockets)
                    Gizmos.DrawWireCube(CellCentre(room.SocketCell(s)), cellSize * 0.6f);

            if (drawFlowGraph)
            {
                foreach (FlowEdge e in _result.flow.edges)
                {
                    Gizmos.color = e.isLoop ? Color.cyan : Color.yellow;
                    Gizmos.DrawLine(RoomCentre(e.from), RoomCentre(e.to));
                }
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(CellCentre(_result.playerStart), 0.4f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(RoomCentre(_result.flow.EndId), 0.4f);

            Gizmos.matrix = previous;
        }

        static Vector3 CellCentre(Vector2Int p) => new Vector3(p.x + 0.5f, p.y + 0.5f, 0f);

        Vector3 RoomCentre(int roomId)
        {
            Vector2 c = _result.rooms[roomId].bounds.center;
            return new Vector3(c.x, c.y, -0.1f);
        }
    }
}
