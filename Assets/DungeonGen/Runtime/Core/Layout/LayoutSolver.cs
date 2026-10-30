using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>
    /// Layout stage: graph-guided incremental placement. Walks the flow tree breadth-first from the start room
    /// and places each child so one of its sockets faces a free parent socket across a straight gap.
    /// Each tree connection reserves its straight corridor so later rooms can't block it.
    /// </summary>
    public static class LayoutSolver
    {
        /// <summary>Corridor gaps vary between roomSpacing and roomSpacing + GapJitter.</summary>
        const int GapJitter = 2;

        /// <param name="candidates">Templates allowed per node id, from <see cref="TemplateAssignment"/>.</param>
        /// <param name="corridors">Tree connections with sockets chosen; paths are filled by the corridor stage.</param>
        public static bool TrySolve(FlowGraph graph, List<RoomTemplate>[] candidates, int roomSpacing, IRandom rng,
            out List<PlacedRoom> rooms, out List<Corridor> corridors, out string failure)
        {
            int spacing = Math.Max(1, roomSpacing);
            int count = graph.nodes.Count;
            rooms = new List<PlacedRoom>(count);
            corridors = new List<Corridor>(count);
            var reservedLines = new List<RectInt>(count);

            var children = new List<int>[count];
            for (int i = 0; i < count; i++) children[i] = new List<int>();
            foreach (FlowEdge e in graph.edges)
                if (!e.isLoop) children[e.from].Add(e.to);

            var placed = new PlacedRoom[count];

            FlowNode startNode = graph.nodes[graph.StartId];
            RoomTemplate startTemplate = WeightedOrder(candidates[startNode.id], rng)[0];
            var start = new PlacedRoom(startNode, startTemplate, -startTemplate.size / 2);
            placed[startNode.id] = start;
            rooms.Add(start);

            var queue = new Queue<int>();
            queue.Enqueue(startNode.id);
            while (queue.Count > 0)
            {
                PlacedRoom parent = placed[queue.Dequeue()];
                foreach (int childId in children[parent.id])
                {
                    FlowNode child = graph.nodes[childId];
                    PlacedRoom room = TryPlaceChild(parent, child, candidates[childId], spacing, rng,
                        rooms, reservedLines, corridors);
                    if (room == null)
                    {
                        failure = $"Could not fit room {child} next to room {parent.node} without overlapping. " +
                                  "More templates, more door sockets or a smaller room spacing make this easier.";
                        return false;
                    }
                    placed[childId] = room;
                    rooms.Add(room);
                    queue.Enqueue(childId);
                }
            }

            rooms.Sort((a, b) => a.id.CompareTo(b.id));
            failure = null;
            return true;
        }

        static PlacedRoom TryPlaceChild(PlacedRoom parent, FlowNode child, List<RoomTemplate> templates, int spacing,
            IRandom rng, List<PlacedRoom> rooms, List<RectInt> reservedLines, List<Corridor> corridors)
        {
            var parentSockets = new List<int>();
            for (int i = 0; i < parent.template.sockets.Count; i++)
                if (!parent.IsSocketUsed(i)) parentSockets.Add(i);
            rng.Shuffle(parentSockets);

            var childSockets = new List<int>();
            foreach (RoomTemplate template in WeightedOrder(templates, rng))
            {
                childSockets.Clear();
                for (int i = 0; i < template.sockets.Count; i++) childSockets.Add(i);
                rng.Shuffle(childSockets);

                foreach (int ps in parentSockets)
                {
                    Direction facing = parent.template.sockets[ps].facing;
                    Vector2Int step = facing.ToVector();
                    foreach (int cs in childSockets)
                    {
                        DoorSocket childSocket = template.sockets[cs];
                        if (childSocket.facing != facing.Opposite()) continue;

                        int gap = spacing + rng.Range(0, GapJitter + 1);
                        Vector2Int parentCell = parent.SocketCell(ps);
                        Vector2Int childCell = parentCell + step * (gap + 1);
                        Vector2Int origin = childCell - childSocket.position;
                        var bounds = new RectInt(origin, template.size);
                        RectInt line = RectUtility.FromCells(parentCell + step, parentCell + step * gap);

                        if (!Fits(bounds, line, parent.id, spacing, rooms, reservedLines)) continue;

                        var room = new PlacedRoom(child, template, origin);
                        parent.usedSockets.Add(ps);
                        room.usedSockets.Add(cs);
                        reservedLines.Add(line);
                        corridors.Add(new Corridor(parent.id, ps, room.id, cs, false));
                        return room;
                    }
                }
            }
            return null;
        }

        static bool Fits(RectInt bounds, RectInt line, int parentId, int spacing,
            List<PlacedRoom> rooms, List<RectInt> reservedLines)
        {
            foreach (PlacedRoom r in rooms)
            {
                if (RectUtility.Expand(r.bounds, spacing).Overlaps(bounds)) return false;
                // The new corridor may only touch its own parent's wall ring (at its exit cell).
                if (r.id != parentId && RectUtility.Expand(r.bounds, 1).Overlaps(line)) return false;
            }

            RectInt withWalls = RectUtility.Expand(bounds, 1);
            foreach (RectInt reserved in reservedLines)
                if (withWalls.Overlaps(reserved)) return false;

            return true;
        }

        /// <summary>Returns templates in a weighted random order (heavier ones tend to come first).</summary>
        static List<RoomTemplate> WeightedOrder(List<RoomTemplate> templates, IRandom rng)
        {
            var remaining = new List<RoomTemplate>(templates);
            var weights = new List<float>(templates.Count);
            var ordered = new List<RoomTemplate>(templates.Count);
            while (remaining.Count > 0)
            {
                weights.Clear();
                foreach (RoomTemplate t in remaining) weights.Add(t.weight);
                int i = rng.PickWeightedIndex(weights);
                ordered.Add(remaining[i]);
                remaining.RemoveAt(i);
            }
            return ordered;
        }
    }
}
