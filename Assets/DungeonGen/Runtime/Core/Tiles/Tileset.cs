using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGen.Core
{
    /// <summary>A tile with a relative chance of being picked.</summary>
    [Serializable]
    public struct WeightedTile
    {
        public TileBase tile;
        [Min(0f)] public float weight;

        public WeightedTile(TileBase tile, float weight = 1f)
        {
            this.tile = tile;
            this.weight = weight;
        }
    }

    /// <summary>
    /// The tiles a dungeon is drawn with. Only <see cref="wallDefault"/> and one floor tile are required;
    /// every other wall role falls back (see <see cref="WallRoleResolver.Fallback"/>) until it finds a tile.
    /// </summary>
    [CreateAssetMenu(menuName = "DungeonGen/Tileset", fileName = "Tileset")]
    public class Tileset : ScriptableObject
    {
        [Header("Floor")]
        public List<WeightedTile> floorTiles = new List<WeightedTile>();
        [Tooltip("Optional. Drawn on the floor cell of each connected door socket.")]
        public TileBase doorTile;

        [Header("Walls")]
        [Tooltip("Required. Used for any wall role that has no tile of its own.")]
        public TileBase wallDefault;
        [Tooltip("Floor below. Falls back to Default.")] public TileBase wallTop;
        [Tooltip("Floor above. Falls back to Top.")] public TileBase wallBottom;
        [Tooltip("Floor to the right. Falls back to Default.")] public TileBase wallLeft;
        [Tooltip("Floor to the left. Falls back to Left.")] public TileBase wallRight;

        [Tooltip("Floor only diagonally down-right. Falls back to Top.")] public TileBase outerTopLeft;
        [Tooltip("Floor only diagonally down-left. Falls back to Top.")] public TileBase outerTopRight;
        [Tooltip("Floor only diagonally up-right. Falls back to Bottom.")] public TileBase outerBottomLeft;
        [Tooltip("Floor only diagonally up-left. Falls back to Bottom.")] public TileBase outerBottomRight;

        [Tooltip("Floor below and to the right. Falls back to Top.")] public TileBase innerTopLeft;
        [Tooltip("Floor below and to the left. Falls back to Top.")] public TileBase innerTopRight;
        [Tooltip("Floor above and to the right. Falls back to Bottom.")] public TileBase innerBottomLeft;
        [Tooltip("Floor above and to the left. Falls back to Bottom.")] public TileBase innerBottomRight;

        [Header("Decoration")]
        [Tooltip("Reserved for decoration placement in a later version.")]
        public List<WeightedTile> decorTiles = new List<WeightedTile>();

        /// <summary>The tile assigned directly to a role, without fallback. May be null.</summary>
        public TileBase GetAssignedWallTile(WallRole role)
        {
            switch (role)
            {
                case WallRole.Default: return wallDefault;
                case WallRole.Top: return wallTop;
                case WallRole.Bottom: return wallBottom;
                case WallRole.Left: return wallLeft;
                case WallRole.Right: return wallRight;
                case WallRole.OuterTopLeft: return outerTopLeft;
                case WallRole.OuterTopRight: return outerTopRight;
                case WallRole.OuterBottomLeft: return outerBottomLeft;
                case WallRole.OuterBottomRight: return outerBottomRight;
                case WallRole.InnerTopLeft: return innerTopLeft;
                case WallRole.InnerTopRight: return innerTopRight;
                case WallRole.InnerBottomLeft: return innerBottomLeft;
                case WallRole.InnerBottomRight: return innerBottomRight;
                default: return null;
            }
        }

        /// <summary>The tile for a role after following fallbacks. Null only if <see cref="wallDefault"/> is empty.</summary>
        public TileBase GetWallTile(WallRole role)
        {
            while (true)
            {
                TileBase tile = GetAssignedWallTile(role);
                if (tile != null || role == WallRole.Default) return tile;
                role = WallRoleResolver.Fallback(role);
            }
        }

        /// <summary>The role whose tile is actually used for <paramref name="role"/>.</summary>
        public WallRole GetEffectiveRole(WallRole role)
        {
            while (role != WallRole.Default && GetAssignedWallTile(role) == null)
                role = WallRoleResolver.Fallback(role);
            return role;
        }

        /// <summary>Picks a floor tile from a value in [0, 1). Returns null if there are no usable floor tiles.</summary>
        public TileBase PickFloorTile(float roll01)
        {
            float total = 0f;
            foreach (WeightedTile w in floorTiles)
                if (w.tile != null && w.weight > 0f) total += w.weight;
            if (total <= 0f) return null;

            float roll = roll01 * total;
            TileBase last = null;
            foreach (WeightedTile w in floorTiles)
            {
                if (w.tile == null || w.weight <= 0f) continue;
                last = w.tile;
                roll -= w.weight;
                if (roll < 0f) return w.tile;
            }
            return last;
        }

        /// <summary>The first usable floor tile, used where floor variation is turned off.</summary>
        public TileBase PrimaryFloorTile => PickFloorTile(0f);

        /// <summary>Problems that stop this tileset from rendering, or null if it can render.</summary>
        public string Validate()
        {
            if (wallDefault == null) return $"Tileset '{name}' needs a Default wall tile.";
            if (PrimaryFloorTile == null) return $"Tileset '{name}' needs at least one floor tile with weight above 0.";
            return null;
        }
    }
}
