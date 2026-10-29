using System.Collections.Generic;
using DungeonGen.Core;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// Development aid: generates a dungeon and shows it in the scene as a sprite, with Gizmo overlays
    /// for the flow graph. With no recipe assigned it uses a built-in set of rectangle rooms.
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

        GameObject _previewObject;
        Texture2D _previewTexture;
        Sprite _previewSprite;
        Material _previewMaterial;

        public DungeonResult Result => _result;

        /// <summary>The current dungeon drawn as a texture, one pixel per cell. Null if generation failed.</summary>
        public Texture2D PreviewTexture => _previewTexture;

        [ContextMenu("Generate")]
        public void Generate()
        {
            DungeonRecipe r = recipe != null ? recipe : GetFallbackRecipe();
            _result = DungeonGenerator.Generate(r, seed);
            if (!_result.success)
                Debug.LogWarning($"[DungeonGen] Seed {seed}: {_result.failureReason}", this);
            RebuildPreview();
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
#if UNITY_EDITOR
            // Creating objects inside OnValidate isn't allowed, so regenerate on the next editor tick.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Generate();
            };
#endif
        }

        void OnDisable()
        {
            DestroyPreview();
            foreach (Object o in _fallbackAssets)
                if (o != null) DestroyImmediate(o);
            _fallbackAssets.Clear();
            _fallbackRecipe = null;
        }

        void RebuildPreview()
        {
            DestroyPreview();
            if (_result == null || !_result.success) return;

            _previewTexture = DungeonPreviewTexture.Create(_result);
            _previewTexture.hideFlags = HideFlags.HideAndDontSave;
            _previewSprite = Sprite.Create(_previewTexture,
                new Rect(0, 0, _previewTexture.width, _previewTexture.height), Vector2.zero, 1f);
            _previewSprite.hideFlags = HideFlags.HideAndDontSave;

            _previewObject = new GameObject("Dungeon Preview") { hideFlags = HideFlags.HideAndDontSave };
            _previewObject.transform.SetParent(transform, false);
            RectInt b = _result.grid.Bounds;
            _previewObject.transform.localPosition = new Vector3(b.xMin, b.yMin, 0f);

            var sr = _previewObject.AddComponent<SpriteRenderer>();
            sr.sprite = _previewSprite;
            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");
            if (unlit != null)
            {
                _previewMaterial = new Material(unlit) { hideFlags = HideFlags.HideAndDontSave };
                sr.sharedMaterial = _previewMaterial;
            }
        }

        void DestroyPreview()
        {
            if (_previewObject != null) DestroyImmediate(_previewObject);
            if (_previewSprite != null) DestroyImmediate(_previewSprite);
            if (_previewTexture != null) DestroyImmediate(_previewTexture);
            if (_previewMaterial != null) DestroyImmediate(_previewMaterial);
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

            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = Color.white;
            foreach (PlacedRoom room in _result.rooms)
                foreach (int s in room.usedSockets)
                    Gizmos.DrawWireCube(CellCentre(room.SocketCell(s)), new Vector3(0.6f, 0.6f, 0.1f));

            if (drawFlowGraph)
            {
                foreach (FlowEdge e in _result.flow.edges)
                {
                    Gizmos.color = e.isLoop ? Color.cyan : Color.yellow;
                    Gizmos.DrawLine(RoomCentre(e.from), RoomCentre(e.to));
                }
            }

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(RoomCentre(_result.flow.EndId), 0.4f);

            Gizmos.matrix = previous;
        }

        static Vector3 CellCentre(Vector2Int p) => new Vector3(p.x + 0.5f, p.y + 0.5f, -0.1f);

        Vector3 RoomCentre(int roomId)
        {
            Vector2 c = _result.rooms[roomId].bounds.center;
            return new Vector3(c.x, c.y, -0.1f);
        }
    }
}
