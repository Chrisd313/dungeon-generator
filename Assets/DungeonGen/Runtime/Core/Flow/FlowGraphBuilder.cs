using System;
using System.Collections.Generic;

namespace DungeonGen.Core
{
    /// <summary>
    /// Flow stage: builds the main path, branches and loop edges, then tags the nodes.
    /// </summary>
    public static class FlowGraphBuilder
    {
        /// <summary>Loop edges only join nodes whose depths differ by at most this much.</summary>
        const int LoopDepthTolerance = 2;

        public static bool TryBuild(DungeonRecipe recipe, IRandom rng, out FlowGraph graph, out string failure)
        {
            graph = null;

            if (!recipe.mainPathLength.IsValid || recipe.mainPathLength.min < 1)
            {
                failure = $"Main path length {recipe.mainPathLength} is invalid; it needs at least 1 room.";
                return false;
            }
            if (!recipe.branchCount.IsValid || !recipe.branchLength.IsValid || !recipe.loopCount.IsValid)
            {
                failure = "Branch count, branch length and loop count ranges must have min <= max.";
                return false;
            }

            graph = new FlowGraph();

            // 1. Main path.
            int mainLength = rng.Range(recipe.mainPathLength);
            for (int i = 0; i < mainLength; i++)
                graph.AddNode(i - 1, i, true);

            // 2. Branches hang off main path nodes, never the start or end.
            int branchCount = rng.Range(recipe.branchCount);
            if (branchCount > 0 && mainLength < 3)
            {
                failure = $"Branches need a main path of at least 3 rooms (start and end can't have branches), but it is {mainLength}.";
                graph = null;
                return false;
            }
            for (int b = 0; b < branchCount; b++)
            {
                int attachIndex = rng.Range(1, mainLength - 1);
                FlowNode parent = graph.nodes[graph.mainPath[attachIndex]];
                int length = Math.Max(1, rng.Range(recipe.branchLength));
                for (int i = 0; i < length; i++)
                    parent = graph.AddNode(parent.id, parent.depth + 1, false);
            }

            // 3. Loops between nodes of similar depth that aren't already neighbours.
            int loopCount = rng.Range(recipe.loopCount);
            if (loopCount > 0)
            {
                var candidates = new List<(int, int)>();
                for (int a = 0; a < graph.nodes.Count; a++)
                for (int b = a + 1; b < graph.nodes.Count; b++)
                {
                    if (Math.Abs(graph.nodes[a].depth - graph.nodes[b].depth) > LoopDepthTolerance) continue;
                    if (graph.AreAdjacent(a, b)) continue;
                    candidates.Add((a, b));
                }
                rng.Shuffle(candidates);
                for (int i = 0; i < loopCount && i < candidates.Count; i++)
                    graph.edges.Add(new FlowEdge(candidates[i].Item1, candidates[i].Item2, true));
            }

            // 4. Tags. Flow rules arrive in a later phase; for now every node gets the fill tag.
            for (int i = 0; i < graph.nodes.Count; i++)
                graph.nodes[i].tag = recipe.defaultFillTag;

            failure = null;
            return true;
        }
    }
}
