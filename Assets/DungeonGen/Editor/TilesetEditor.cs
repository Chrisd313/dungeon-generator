using System;
using System.Text;
using DungeonGen.Core;
using UnityEditor;

namespace DungeonGen.Editor
{
    /// <summary>Tileset inspector that shows which wall roles have their own tile and which fall back.</summary>
    [CustomEditor(typeof(Tileset))]
    public class TilesetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var tileset = (Tileset)target;

            EditorGUILayout.Space();
            string problem = tileset.Validate();
            if (problem != null) EditorGUILayout.HelpBox(problem, MessageType.Error);

            var sb = new StringBuilder("Wall roles:\n");
            foreach (WallRole role in (WallRole[])Enum.GetValues(typeof(WallRole)))
            {
                sb.Append("  ").Append(ObjectNames.NicifyVariableName(role.ToString())).Append(": ");
                if (tileset.GetAssignedWallTile(role) != null) sb.Append("assigned");
                else if (role == WallRole.Default) sb.Append("MISSING");
                else
                {
                    WallRole used = tileset.GetEffectiveRole(role);
                    sb.Append("uses ").Append(ObjectNames.NicifyVariableName(used.ToString()));
                    if (tileset.GetAssignedWallTile(used) == null) sb.Append(" (missing)");
                }
                sb.Append('\n');
            }
            EditorGUILayout.HelpBox(sb.ToString().TrimEnd(), MessageType.None);
        }
    }
}
