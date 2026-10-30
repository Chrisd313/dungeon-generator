namespace DungeonGen.Core
{
    /// <summary>
    /// What a wall cell looks like, decided by where floor is around it.
    /// "Top" means the wall sits above floor (the room's north wall), and so on.
    /// </summary>
    public enum WallRole
    {
        /// <summary>Fallback for every role, and the only one used when floor is on opposite sides or 3+ sides.</summary>
        Default = 0,

        /// <summary>Floor below.</summary>
        Top,
        /// <summary>Floor above.</summary>
        Bottom,
        /// <summary>Floor to the right.</summary>
        Left,
        /// <summary>Floor to the left.</summary>
        Right,

        /// <summary>Convex room corner: floor only diagonally down-right.</summary>
        OuterTopLeft,
        /// <summary>Convex room corner: floor only diagonally down-left.</summary>
        OuterTopRight,
        /// <summary>Convex room corner: floor only diagonally up-right.</summary>
        OuterBottomLeft,
        /// <summary>Convex room corner: floor only diagonally up-left.</summary>
        OuterBottomRight,

        /// <summary>Concave corner: floor below and to the right.</summary>
        InnerTopLeft,
        /// <summary>Concave corner: floor below and to the left.</summary>
        InnerTopRight,
        /// <summary>Concave corner: floor above and to the right.</summary>
        InnerBottomLeft,
        /// <summary>Concave corner: floor above and to the left.</summary>
        InnerBottomRight,
    }

    /// <summary>Maps a wall cell's floor neighbours to a <see cref="WallRole"/>.</summary>
    public static class WallRoleResolver
    {
        // Bit positions follow DungeonGrid.Neighbours8: N, NE, E, SE, S, SW, W, NW.
        const int N = 1 << 0, NE = 1 << 1, E = 1 << 2, SE = 1 << 3, S = 1 << 4, SW = 1 << 5, W = 1 << 6, NW = 1 << 7;

        public const int RoleCount = 13;

        /// <param name="floorMask8">Bitmask of floor neighbours from <see cref="DungeonGrid.NeighbourMask8"/>.</param>
        public static WallRole Resolve(int floorMask8)
        {
            bool n = (floorMask8 & N) != 0, e = (floorMask8 & E) != 0;
            bool s = (floorMask8 & S) != 0, w = (floorMask8 & W) != 0;
            int orthogonal = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0);

            switch (orthogonal)
            {
                case 0:
                    switch (floorMask8 & (NE | SE | SW | NW))
                    {
                        case SE: return WallRole.OuterTopLeft;
                        case SW: return WallRole.OuterTopRight;
                        case NE: return WallRole.OuterBottomLeft;
                        case NW: return WallRole.OuterBottomRight;
                        default: return WallRole.Default;
                    }
                case 1:
                    if (s) return WallRole.Top;
                    if (n) return WallRole.Bottom;
                    if (e) return WallRole.Left;
                    return WallRole.Right;
                case 2:
                    if (s && e) return WallRole.InnerTopLeft;
                    if (s && w) return WallRole.InnerTopRight;
                    if (n && e) return WallRole.InnerBottomLeft;
                    if (n && w) return WallRole.InnerBottomRight;
                    return WallRole.Default; // floor on opposite sides: a thin wall
                default:
                    return WallRole.Default;
            }
        }

        /// <summary>
        /// The role to try when a tileset has no tile for <paramref name="role"/>.
        /// Every chain ends at <see cref="WallRole.Default"/>.
        /// </summary>
        public static WallRole Fallback(WallRole role)
        {
            switch (role)
            {
                case WallRole.Bottom: return WallRole.Top;
                case WallRole.Right: return WallRole.Left;
                case WallRole.OuterTopLeft:
                case WallRole.OuterTopRight:
                case WallRole.InnerTopLeft:
                case WallRole.InnerTopRight:
                    return WallRole.Top;
                case WallRole.OuterBottomLeft:
                case WallRole.OuterBottomRight:
                case WallRole.InnerBottomLeft:
                case WallRole.InnerBottomRight:
                    return WallRole.Bottom;
                default:
                    return WallRole.Default;
            }
        }
    }
}
