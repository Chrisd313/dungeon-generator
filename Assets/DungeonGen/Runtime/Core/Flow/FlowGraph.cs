using System.Collections.Generic;

namespace DungeonGen.Core
{
    /// <summary>An abstract room slot in the dungeon's structure.</summary>
    public sealed class FlowNode
    {
        public int id;
        public string tag;
        /// <summary>Steps from the start room along tree edges.</summary>
        public int depth;
        public bool isMainPath;
        /// <summary>Tree parent, or -1 for the start node.</summary>
        public int parentId;
        /// <summary>Position on the main path (0 = start), or -1 for branch nodes.</summary>
        public int mainPathIndex = -1;

        public override string ToString() => $"#{id} '{tag}' depth {depth}";
    }

    /// <summary>A connection between two flow nodes. Tree edges run parent to child.</summary>
    public struct FlowEdge
    {
        public int from;
        public int to;
        public bool isLoop;

        public FlowEdge(int from, int to, bool isLoop)
        {
            this.from = from;
            this.to = to;
            this.isLoop = isLoop;
        }
    }

    /// <summary>The dungeon's abstract structure: rooms and how they connect, before any positions exist.</summary>
    public sealed class FlowGraph
    {
        public readonly List<FlowNode> nodes = new List<FlowNode>();
        public readonly List<FlowEdge> edges = new List<FlowEdge>();

        /// <summary>Main path node ids in order, start first.</summary>
        public readonly List<int> mainPath = new List<int>();

        public FlowNode AddNode(int parentId, int depth, bool isMainPath)
        {
            var node = new FlowNode
            {
                id = nodes.Count,
                parentId = parentId,
                depth = depth,
                isMainPath = isMainPath,
            };
            nodes.Add(node);
            if (isMainPath)
            {
                node.mainPathIndex = mainPath.Count;
                mainPath.Add(node.id);
            }
            if (parentId >= 0) edges.Add(new FlowEdge(parentId, node.id, false));
            return node;
        }

        public bool AreAdjacent(int a, int b)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                FlowEdge e = edges[i];
                if ((e.from == a && e.to == b) || (e.from == b && e.to == a)) return true;
            }
            return false;
        }

        /// <summary>Number of edges touching a node.</summary>
        public int Degree(int id, bool includeLoops)
        {
            int n = 0;
            for (int i = 0; i < edges.Count; i++)
            {
                FlowEdge e = edges[i];
                if (e.isLoop && !includeLoops) continue;
                if (e.from == id || e.to == id) n++;
            }
            return n;
        }

        public int StartId => mainPath.Count > 0 ? mainPath[0] : -1;
        public int EndId => mainPath.Count > 0 ? mainPath[mainPath.Count - 1] : -1;
    }
}
