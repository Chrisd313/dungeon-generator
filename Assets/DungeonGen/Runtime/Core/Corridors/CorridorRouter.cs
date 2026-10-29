using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>
    /// Corridor stage: routes every tree connection chosen by the layout, then tries to add loop corridors.
    /// Rooms and their wall ring are impassable; only the exit cells of connected sockets open into a room.
    /// </summary>
    public static class CorridorRouter
    {
        /// <summary>Free space around the rooms that loop corridors may detour through.</summary>
        const int SearchMargin = 8;

        /// <summary>Socket pairs tried per loop edge before giving up on it.</summary>
        const int LoopPairsToTry = 4;

        /// <param name="rooms">Placed rooms, indexed by id.</param>
        /// <param name="corridors">Tree corridors from the layout stage; loop corridors are appended.</param>
        public static bool TryRoute(FlowGraph graph, List<PlacedRoom> rooms, List<Corridor> corridors,
            CorridorSettings settings, IRandom rng, out string failure)
        {
            RectInt area = rooms[0].bounds;
            foreach (PlacedRoom r in rooms) area = RectUtility.Union(area, r.bounds);
            var finder = new CorridorPathfinder(RectUtility.Expand(area, SearchMargin));
            foreach (PlacedRoom r in rooms) finder.Block(RectUtility.Expand(r.bounds, 1));

            foreach (Corridor c in corridors)
            {
                PlacedRoom from = rooms[c.fromRoom], to = rooms[c.toRoom];
                Vector2Int a = from.ExitCell(c.fromSocket), b = to.ExitCell(c.toSocket);
                finder.SetBlocked(a, false);
                finder.SetBlocked(b, false);
                c.path = finder.FindPath(a, from.template.sockets[c.fromSocket].facing, b, settings, int.MaxValue);
                if (c.path == null)
                {
                    failure = $"No corridor route between room {from.node} and room {to.node}.";
                    return false;
                }
                finder.MarkCorridor(c.path);
            }

            foreach (FlowEdge e in graph.edges)
            {
                if (!e.isLoop) continue;
                Corridor loop = TryRouteLoop(rooms[e.from], rooms[e.to], rooms, finder, settings);
                if (loop != null) corridors.Add(loop);
            }

            failure = null;
            return true;
        }

        static Corridor TryRouteLoop(PlacedRoom a, PlacedRoom b, List<PlacedRoom> rooms,
            CorridorPathfinder finder, CorridorSettings settings)
        {
            var pairs = new List<(int sa, int sb, int distance)>();
            for (int sa = 0; sa < a.template.sockets.Count; sa++)
            {
                if (a.IsSocketUsed(sa) || !ExitIsClear(a, sa, rooms, finder)) continue;
                for (int sb = 0; sb < b.template.sockets.Count; sb++)
                {
                    if (b.IsSocketUsed(sb) || !ExitIsClear(b, sb, rooms, finder)) continue;
                    Vector2Int d = a.ExitCell(sa) - b.ExitCell(sb);
                    pairs.Add((sa, sb, Math.Abs(d.x) + Math.Abs(d.y)));
                }
            }
            // Stable sort: equal distances keep socket order, so this stays deterministic.
            pairs.Sort((p, q) => p.distance != q.distance ? p.distance.CompareTo(q.distance)
                : p.sa != q.sa ? p.sa.CompareTo(q.sa) : p.sb.CompareTo(q.sb));

            for (int i = 0; i < pairs.Count && i < LoopPairsToTry; i++)
            {
                var (sa, sb, _) = pairs[i];
                Vector2Int exitA = a.ExitCell(sa), exitB = b.ExitCell(sb);
                if (exitA == exitB) continue;

                finder.SetBlocked(exitA, false);
                finder.SetBlocked(exitB, false);
                List<Vector2Int> path = finder.FindPath(exitA, a.template.sockets[sa].facing, exitB,
                    settings, settings.maxLength, maxExpansions: 50000);
                if (path != null)
                {
                    finder.MarkCorridor(path);
                    a.usedSockets.Add(sa);
                    b.usedSockets.Add(sb);
                    return new Corridor(a.id, sa, b.id, sb, true) { path = path };
                }
                finder.SetBlocked(exitA, true);
                finder.SetBlocked(exitB, true);
            }
            return null;
        }

        /// <summary>A socket can take a loop corridor only if its exit doesn't touch another room's walls.</summary>
        static bool ExitIsClear(PlacedRoom owner, int socket, List<PlacedRoom> rooms, CorridorPathfinder finder)
        {
            Vector2Int exit = owner.ExitCell(socket);
            if (!finder.InBounds(exit)) return false;
            foreach (PlacedRoom r in rooms)
                if (r != owner && RectUtility.Contains(RectUtility.Expand(r.bounds, 1), exit))
                    return false;
            return true;
        }
    }
}
