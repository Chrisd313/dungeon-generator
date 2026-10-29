using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>A carved path between two room sockets.</summary>
    public sealed class Corridor
    {
        public int fromRoom;
        public int toRoom;
        public int fromSocket;
        public int toSocket;
        public bool isLoop;

        /// <summary>Corridor cells from the exit of <see cref="fromSocket"/> to the exit of <see cref="toSocket"/>, inclusive.</summary>
        public List<Vector2Int> path;

        public Corridor(int fromRoom, int fromSocket, int toRoom, int toSocket, bool isLoop)
        {
            this.fromRoom = fromRoom;
            this.fromSocket = fromSocket;
            this.toRoom = toRoom;
            this.toSocket = toSocket;
            this.isLoop = isLoop;
        }
    }
}
