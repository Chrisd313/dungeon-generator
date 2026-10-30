using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>A room template positioned on the dungeon grid.</summary>
    public sealed class PlacedRoom
    {
        /// <summary>Same as the flow node id.</summary>
        public int id;
        public FlowNode node;
        public RoomTemplate template;
        public Vector2Int origin;
        public RectInt bounds;

        /// <summary>Indices into <c>template.sockets</c> that have a corridor attached.</summary>
        public readonly List<int> usedSockets = new List<int>();

        public PlacedRoom(FlowNode node, RoomTemplate template, Vector2Int origin)
        {
            id = node.id;
            this.node = node;
            this.template = template;
            this.origin = origin;
            bounds = new RectInt(origin, template.size);
        }

        public bool IsSocketUsed(int socketIndex) => usedSockets.Contains(socketIndex);

        /// <summary>Grid position of a socket's floor cell.</summary>
        public Vector2Int SocketCell(int socketIndex) => origin + template.sockets[socketIndex].position;

        /// <summary>Grid position of the cell just outside a socket, where its corridor starts.</summary>
        public Vector2Int ExitCell(int socketIndex)
        {
            DoorSocket s = template.sockets[socketIndex];
            return origin + s.position + s.facing.ToVector();
        }
    }
}
