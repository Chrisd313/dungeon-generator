using System.Collections.Generic;

namespace DungeonGen.Core
{
    /// <summary>
    /// Assignment stage: finds which templates may fill each flow node. The layout stage then
    /// picks among them by weight, falling back to other candidates when one doesn't fit.
    /// </summary>
    public static class TemplateAssignment
    {
        /// <returns>Candidate lists indexed by node id.</returns>
        public static bool TryAssign(FlowGraph graph, RoomTemplateLibrary library,
            out List<RoomTemplate>[] candidates, out string failure)
        {
            candidates = new List<RoomTemplate>[graph.nodes.Count];
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                FlowNode node = graph.nodes[i];
                int minSockets = graph.Degree(node.id, includeLoops: false);
                var list = new List<RoomTemplate>();
                library.GetCandidates(node.tag, node.depth, minSockets, list);
                if (list.Count == 0)
                {
                    failure = $"No room template has tag '{node.tag}', allows depth {node.depth} " +
                              $"and has at least {minSockets} door socket(s) (needed by room {node.id}).";
                    return false;
                }
                candidates[i] = list;
            }
            failure = null;
            return true;
        }
    }
}
