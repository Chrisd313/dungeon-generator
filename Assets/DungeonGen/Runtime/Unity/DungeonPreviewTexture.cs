using DungeonGen.Core;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>Renders a dungeon grid into a texture: walls dark, corridors tan, rooms tinted by id.</summary>
    public static class DungeonPreviewTexture
    {
        static readonly Color32 EmptyColor = new Color32(13, 13, 15, 255);
        static readonly Color32 WallColor = new Color32(51, 51, 61, 255);
        static readonly Color32 CorridorColor = new Color32(204, 191, 153, 255);
        static readonly Color32 StartColor = new Color32(40, 220, 60, 255);

        public static Color32 RoomColor(int roomId) => Color.HSVToRGB(roomId * 0.618034f % 1f, 0.45f, 0.9f);

        /// <summary>Creates a point-filtered texture with <paramref name="pixelsPerCell"/> pixels per grid cell.</summary>
        public static Texture2D Create(DungeonResult result, int pixelsPerCell = 1)
        {
            DungeonGrid grid = result.grid;
            var tex = new Texture2D(grid.Width * pixelsPerCell, grid.Height * pixelsPerCell, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Dungeon {result.seed}",
            };
            var pixels = new Color32[tex.width * tex.height];
            RectInt b = grid.Bounds;

            for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
            {
                var p = new Vector2Int(x, y);
                Color32 c;
                switch (grid.Get(p))
                {
                    case CellType.Wall: c = WallColor; break;
                    case CellType.Floor:
                        int roomId = grid.GetRoomId(p);
                        c = roomId < 0 ? CorridorColor : RoomColor(roomId);
                        break;
                    default: c = EmptyColor; break;
                }
                if (p == result.playerStart) c = StartColor;

                int px = (x - b.xMin) * pixelsPerCell, py = (y - b.yMin) * pixelsPerCell;
                for (int dy = 0; dy < pixelsPerCell; dy++)
                for (int dx = 0; dx < pixelsPerCell; dx++)
                    pixels[(py + dy) * tex.width + px + dx] = c;
            }

            tex.SetPixels32(pixels);
            tex.Apply(false);
            return tex;
        }
    }
}
