using System;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>
    /// A place on a room template's edge where a corridor may attach.
    /// <see cref="position"/> is a floor cell on the template edge; the corridor starts on the cell beyond it in <see cref="facing"/>.
    /// </summary>
    [Serializable]
    public struct DoorSocket
    {
        public Vector2Int position;
        public Direction facing;
        public int width;

        public DoorSocket(Vector2Int position, Direction facing, int width = 1)
        {
            this.position = position;
            this.facing = facing;
            this.width = width;
        }
    }
}
