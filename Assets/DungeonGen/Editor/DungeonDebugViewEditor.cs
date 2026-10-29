using DungeonGen.Core;
using UnityEditor;
using UnityEngine;

namespace DungeonGen.Editor
{
    [CustomEditor(typeof(DungeonDebugView))]
    public class DungeonDebugViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var view = (DungeonDebugView)target;
            DungeonResult r = view.Result;

            EditorGUILayout.Space();
            if (r == null)
                EditorGUILayout.HelpBox("Not generated yet.", MessageType.Info);
            else if (!r.success)
                EditorGUILayout.HelpBox(r.failureReason, MessageType.Error);
            else
                EditorGUILayout.HelpBox(
                    $"{r.rooms.Count} rooms, {r.corridors.Count} corridors, grid {r.grid.Bounds}, " +
                    $"{r.attempts} attempt(s).", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate")) Regenerate(view, view.seed);
                if (GUILayout.Button("Next Seed")) Regenerate(view, view.seed + 1);
                using (new EditorGUI.DisabledScope(r == null || !r.success))
                    if (GUILayout.Button("Frame Dungeon")) Frame(view);
            }
        }

        static void Regenerate(DungeonDebugView view, int seed)
        {
            Undo.RecordObject(view, "Generate Dungeon");
            view.seed = seed;
            view.Generate();
            SceneView.RepaintAll();
        }

        static void Frame(DungeonDebugView view)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;
            RectInt b = view.Result.grid.Bounds;
            Vector3 centre = view.transform.TransformPoint(new Vector3(b.center.x, b.center.y, 0f));
            Vector3 size = view.transform.TransformVector(new Vector3(b.width, b.height, 1f));
            sceneView.Frame(new Bounds(centre, size), false);
        }
    }
}
