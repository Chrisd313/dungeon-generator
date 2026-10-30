using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>
    /// Entry point for pure generation. Same recipe + seed always gives the same dungeon.
    /// Never throws for bad input and never hangs: failures come back as <see cref="DungeonResult.failureReason"/>.
    /// </summary>
    public static class DungeonGenerator
    {
        public static DungeonResult Generate(DungeonRecipe recipe, int seed)
        {
            if (recipe == null) return DungeonResult.Fail(seed, "No recipe assigned.");
            if (recipe.templates == null) return DungeonResult.Fail(seed, "Recipe has no room template library.");
            if (recipe.corridors == null) return DungeonResult.Fail(seed, "Recipe has no corridor settings.");

            if (!FlowGraphBuilder.TryBuild(recipe, SeedUtility.CreateRandom(seed, GenerationStage.Flow),
                    out FlowGraph flow, out string failure))
                return DungeonResult.Fail(seed, failure);

            if (!TemplateAssignment.TryAssign(flow, recipe.templates, out List<RoomTemplate>[] candidates, out failure))
                return DungeonResult.Fail(seed, failure);

            int maxAttempts = Mathf.Max(1, recipe.maxAttempts);
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                IRandom layoutRng = SeedUtility.CreateRandom(seed, GenerationStage.Layout, attempt);
                if (!LayoutSolver.TrySolve(flow, candidates, recipe.roomSpacing, layoutRng,
                        out List<PlacedRoom> rooms, out List<Corridor> corridors, out failure))
                    continue;

                IRandom corridorRng = SeedUtility.CreateRandom(seed, GenerationStage.Corridors, attempt);
                if (!CorridorRouter.TryRoute(flow, rooms, corridors, recipe.corridors, corridorRng, out failure))
                    continue;

                DungeonGrid grid = GridBuilder.Build(rooms, corridors);
                Vector2Int start = FindPlayerStart(rooms[flow.StartId], grid);

                if (!ConnectivityValidator.Validate(grid, start, out failure))
                    continue;

                return new DungeonResult
                {
                    success = true,
                    seed = seed,
                    attempts = attempt + 1,
                    flow = flow,
                    rooms = rooms,
                    corridors = corridors,
                    grid = grid,
                    playerStart = start,
                };
            }

            return DungeonResult.Fail(seed,
                $"Layout failed after {maxAttempts} attempt(s). Last problem: {failure}", maxAttempts);
        }

        /// <summary>The start room's player-start zone if it has one, else its floor cell closest to the centre.</summary>
        static Vector2Int FindPlayerStart(PlacedRoom startRoom, DungeonGrid grid)
        {
            foreach (SpawnZone zone in startRoom.template.spawnZones)
            {
                if (zone == null || !zone.isPlayerStart) continue;
                foreach (Vector2Int local in zone.cells)
                    if (grid.Get(startRoom.origin + local) == CellType.Floor)
                        return startRoom.origin + local;
            }

            Vector2 centre = startRoom.bounds.center;
            Vector2Int best = startRoom.origin;
            float bestDistance = float.MaxValue;
            RectInt b = startRoom.bounds;
            for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
            {
                var p = new Vector2Int(x, y);
                if (grid.Get(p) != CellType.Floor || grid.GetRoomId(p) != startRoom.id) continue;
                float d = (new Vector2(x + 0.5f, y + 0.5f) - centre).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }
            return best;
        }
    }
}
