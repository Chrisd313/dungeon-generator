using System.IO;
using DungeonGen.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGen.Editor
{
    /// <summary>
    /// Generates a simple placeholder tileset (flat colours with a light edge facing the floor)
    /// so dungeons can be rendered before real art exists.
    /// </summary>
    public static class PlaceholderTilesetGenerator
    {
        public const string Folder = "Assets/DungeonGen Placeholder";
        const int Size = 16;
        const int Edge = 4;

        static readonly Color32 WallBase = new Color32(58, 52, 70, 255);
        static readonly Color32 WallFace = new Color32(150, 138, 165, 255);
        static readonly Color32 WallBorder = new Color32(38, 34, 46, 255);
        static readonly Color32 Door = new Color32(140, 90, 50, 255);
        static readonly Color32[] FloorColors =
        {
            new Color32(120, 112, 96, 255),
            new Color32(112, 105, 90, 255),
            new Color32(128, 118, 100, 255),
        };

        [System.Flags]
        enum Sides { None = 0, Top = 1, Bottom = 2, Left = 4, Right = 8 }

        enum Corner { None, TopLeft, TopRight, BottomLeft, BottomRight }

        [MenuItem("Tools/DungeonGen/Create Placeholder Tileset")]
        static void CreateFromMenu()
        {
            Tileset tileset = GetOrCreate();
            Selection.activeObject = tileset;
            EditorGUIUtility.PingObject(tileset);
        }

        /// <summary>Returns the placeholder tileset, generating it on first use.</summary>
        public static Tileset GetOrCreate()
        {
            string tilesetPath = $"{Folder}/Placeholder Tileset.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Tileset>(tilesetPath);
            if (existing != null) return existing;

            Directory.CreateDirectory(Folder);
            var tileset = ScriptableObject.CreateInstance<Tileset>();

            for (int i = 0; i < FloorColors.Length; i++)
                tileset.floorTiles.Add(new WeightedTile(MakeTile($"Floor {i + 1}", Floor(FloorColors[i], i), false),
                    i == 0 ? 4f : 1f));
            tileset.doorTile = MakeTile("Door", Solid(Door), false);

            tileset.wallDefault = MakeTile("Wall Default", Wall(Sides.None, Corner.None), true);
            tileset.wallTop = MakeTile("Wall Top", Wall(Sides.Bottom, Corner.None), true);
            tileset.wallBottom = MakeTile("Wall Bottom", Wall(Sides.Top, Corner.None), true);
            tileset.wallLeft = MakeTile("Wall Left", Wall(Sides.Right, Corner.None), true);
            tileset.wallRight = MakeTile("Wall Right", Wall(Sides.Left, Corner.None), true);
            tileset.outerTopLeft = MakeTile("Wall Outer Top Left", Wall(Sides.None, Corner.BottomRight), true);
            tileset.outerTopRight = MakeTile("Wall Outer Top Right", Wall(Sides.None, Corner.BottomLeft), true);
            tileset.outerBottomLeft = MakeTile("Wall Outer Bottom Left", Wall(Sides.None, Corner.TopRight), true);
            tileset.outerBottomRight = MakeTile("Wall Outer Bottom Right", Wall(Sides.None, Corner.TopLeft), true);
            tileset.innerTopLeft = MakeTile("Wall Inner Top Left", Wall(Sides.Bottom | Sides.Right, Corner.None), true);
            tileset.innerTopRight = MakeTile("Wall Inner Top Right", Wall(Sides.Bottom | Sides.Left, Corner.None), true);
            tileset.innerBottomLeft = MakeTile("Wall Inner Bottom Left", Wall(Sides.Top | Sides.Right, Corner.None), true);
            tileset.innerBottomRight = MakeTile("Wall Inner Bottom Right", Wall(Sides.Top | Sides.Left, Corner.None), true);

            AssetDatabase.CreateAsset(tileset, tilesetPath);
            AssetDatabase.SaveAssets();
            return tileset;
        }

        static Tile MakeTile(string name, Color32[] pixels, bool solid)
        {
            string pngPath = $"{Folder}/{name}.png";
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(pngPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Size;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, $"{Folder}/{name}.asset");
            return tile;
        }

        static Color32[] Solid(Color32 c)
        {
            var pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            return pixels;
        }

        static Color32[] Floor(Color32 c, int variant)
        {
            Color32[] pixels = Solid(c);
            // A few darker specks so variants are distinguishable.
            for (int i = 0; i < 6; i++)
            {
                int x = (i * 7 + variant * 5) % Size, y = (i * 11 + variant * 3) % Size;
                pixels[y * Size + x] = new Color32((byte)(c.r - 20), (byte)(c.g - 20), (byte)(c.b - 20), 255);
            }
            return pixels;
        }

        /// <summary>Dark wall with a light face on the sides (or corner) that face floor.</summary>
        static Color32[] Wall(Sides faces, Corner corner)
        {
            Color32[] pixels = Solid(WallBase);
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                bool face = ((faces & Sides.Bottom) != 0 && y < Edge)
                            || ((faces & Sides.Top) != 0 && y >= Size - Edge)
                            || ((faces & Sides.Left) != 0 && x < Edge)
                            || ((faces & Sides.Right) != 0 && x >= Size - Edge)
                            || (corner == Corner.BottomRight && x >= Size - Edge && y < Edge)
                            || (corner == Corner.BottomLeft && x < Edge && y < Edge)
                            || (corner == Corner.TopRight && x >= Size - Edge && y >= Size - Edge)
                            || (corner == Corner.TopLeft && x < Edge && y >= Size - Edge);
                bool border = x == 0 || y == 0 || x == Size - 1 || y == Size - 1;
                if (face) pixels[y * Size + x] = WallFace;
                else if (border) pixels[y * Size + x] = WallBorder;
            }
            return pixels;
        }
    }
}
