using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGen.Editor
{
    /// <summary>One-click scene setup: a Grid with floor and wall tilemaps driven by a Dungeon Debug View.</summary>
    public static class DebugSceneSetup
    {
        [MenuItem("Tools/DungeonGen/Create Debug Dungeon In Scene")]
        static void Create()
        {
            var tileset = PlaceholderTilesetGenerator.GetOrCreate();

            var gridObject = new GameObject("Dungeon Grid", typeof(Grid));
            Undo.RegisterCreatedObjectUndo(gridObject, "Create Debug Dungeon");
            Tilemap floor = CreateTilemap("Floor", gridObject.transform, 0);
            Tilemap walls = CreateTilemap("Walls", gridObject.transform, 1);

            var view = gridObject.AddComponent<DungeonDebugView>();
            view.tileset = tileset;
            view.floorTilemap = floor;
            view.wallTilemap = walls;
            view.showPreviewSprite = false;
            view.Generate();

            Selection.activeGameObject = gridObject;
        }

        static Tilemap CreateTilemap(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return go.GetComponent<Tilemap>();
        }
    }
}
