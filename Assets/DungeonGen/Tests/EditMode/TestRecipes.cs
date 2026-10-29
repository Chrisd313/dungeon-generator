using System.Collections.Generic;
using System.Text;
using DungeonGen.Core;
using UnityEngine;

namespace DungeonGen.Tests
{
    /// <summary>Builds throwaway recipes from rectangle templates. Call <see cref="Destroy"/> when done.</summary>
    internal sealed class TestRecipes
    {
        readonly List<Object> _created = new List<Object>();

        public DungeonRecipe Create(int mainPath = 9, int branches = 2, int branchLength = 3, int loops = 2,
            int roomSpacing = 3)
        {
            var library = Track(ScriptableObject.CreateInstance<RoomTemplateLibrary>());
            Vector2Int[] sizes =
            {
                new Vector2Int(5, 5), new Vector2Int(7, 5), new Vector2Int(5, 7), new Vector2Int(7, 7),
                new Vector2Int(9, 7), new Vector2Int(7, 9), new Vector2Int(11, 9),
            };
            foreach (Vector2Int s in sizes)
                library.templates.Add(Track(RoomTemplate.CreateRectangle(s.x, s.y, "combat")));

            var recipe = Track(ScriptableObject.CreateInstance<DungeonRecipe>());
            recipe.mainPathLength = new IntRange(mainPath, mainPath);
            recipe.branchCount = new IntRange(branches, branches);
            recipe.branchLength = new IntRange(branchLength, branchLength);
            recipe.loopCount = new IntRange(loops, loops);
            recipe.defaultFillTag = "combat";
            recipe.templates = library;
            recipe.roomSpacing = roomSpacing;
            recipe.maxAttempts = 20;
            return recipe;
        }

        public T Track<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }

        public void Destroy()
        {
            foreach (Object o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        /// <summary>Stable text fingerprint of a result, for determinism checks.</summary>
        public static string Fingerprint(DungeonResult r)
        {
            var sb = new StringBuilder();
            sb.Append(r.success).Append('|').Append(r.attempts).Append('|');
            foreach (PlacedRoom room in r.rooms)
                sb.Append(room.id).Append(':').Append(room.template.name).Append('@').Append(room.origin).Append(';');
            foreach (Corridor c in r.corridors)
            {
                sb.Append(c.fromRoom).Append('-').Append(c.toRoom).Append(c.isLoop ? "L" : "").Append(':');
                foreach (Vector2Int p in c.path) sb.Append(p.x).Append(',').Append(p.y).Append(' ');
                sb.Append(';');
            }
            sb.Append(r.playerStart);
            return sb.ToString();
        }
    }
}
