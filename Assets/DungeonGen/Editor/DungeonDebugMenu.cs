using System.Diagnostics;
using System.IO;
using DungeonGen.Core;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DungeonGen.Editor
{
    /// <summary>Developer menu items: stress-test a recipe and export grid snapshots.</summary>
    public static class DungeonDebugMenu
    {
        const int StressSeeds = 1000;

        [MenuItem("Tools/DungeonGen/Stress Test Selected Recipe")]
        static void StressTestSelected()
        {
            var recipe = Selection.activeObject as DungeonRecipe;
            if (recipe == null)
            {
                Debug.LogWarning("[DungeonGen] Select a Dungeon Recipe asset first.");
                return;
            }
            StressTest(recipe, StressSeeds);
        }

        /// <summary>Generates <paramref name="seeds"/> dungeons and logs success rate and timing.</summary>
        public static void StressTest(DungeonRecipe recipe, int seeds)
        {
            int ok = 0, attempts = 0;
            double slowest = 0;
            string firstFailure = null;
            var total = Stopwatch.StartNew();
            var each = new Stopwatch();

            for (int seed = 0; seed < seeds; seed++)
            {
                each.Restart();
                DungeonResult r = DungeonGenerator.Generate(recipe, seed);
                each.Stop();
                slowest = System.Math.Max(slowest, each.Elapsed.TotalMilliseconds);
                if (r.success)
                {
                    ok++;
                    attempts += r.attempts;
                }
                else if (firstFailure == null) firstFailure = $"seed {seed}: {r.failureReason}";
            }

            Debug.Log($"[DungeonGen] Stress '{recipe.name}': {ok}/{seeds} succeeded ({100f * ok / seeds:0.0}%), " +
                      $"avg attempts {(float)attempts / Mathf.Max(1, ok):0.00}, " +
                      $"avg {total.Elapsed.TotalMilliseconds / seeds:0.00} ms, slowest {slowest:0.00} ms" +
                      (firstFailure != null ? $"\nFirst failure: {firstFailure}" : ""));
        }

        /// <summary>Writes a dungeon grid as a PNG, 4 pixels per cell. Rooms are tinted by id.</summary>
        public static void ExportPng(DungeonResult result, string path, int pixelsPerCell = 4)
        {
            DungeonGrid grid = result.grid;
            var tex = new Texture2D(grid.Width * pixelsPerCell, grid.Height * pixelsPerCell, TextureFormat.RGBA32, false);
            var pixels = new Color32[tex.width * tex.height];
            RectInt b = grid.Bounds;

            for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
            {
                var p = new Vector2Int(x, y);
                Color c;
                switch (grid.Get(p))
                {
                    case CellType.Wall: c = new Color(0.2f, 0.2f, 0.24f); break;
                    case CellType.Floor:
                        int roomId = grid.GetRoomId(p);
                        c = roomId < 0 ? new Color(0.8f, 0.75f, 0.6f) : Color.HSVToRGB(roomId * 0.618034f % 1f, 0.45f, 0.9f);
                        break;
                    default: c = new Color(0.05f, 0.05f, 0.06f); break;
                }
                if (p == result.playerStart) c = Color.green;

                int px = (x - b.xMin) * pixelsPerCell, py = (y - b.yMin) * pixelsPerCell;
                for (int dy = 0; dy < pixelsPerCell; dy++)
                for (int dx = 0; dx < pixelsPerCell; dx++)
                    pixels[(py + dy) * tex.width + px + dx] = c;
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// Batch-mode entry point: <c>-executeMethod DungeonGen.Editor.DungeonDebugMenu.ExportSamples</c>.
        /// Writes a few seeds of the built-in rectangle recipe to Logs/DungeonSnapshots.
        /// </summary>
        public static void ExportSamples()
        {
            var view = new GameObject("DebugView").AddComponent<DungeonDebugView>();
            try
            {
                for (int seed = 0; seed < 4; seed++)
                {
                    view.seed = seed;
                    view.Generate();
                    if (view.Result.success)
                        ExportPng(view.Result, $"Logs/DungeonSnapshots/seed_{seed}.png");
                }
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }
    }
}
