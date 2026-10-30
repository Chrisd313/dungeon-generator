# DungeonGen

Procedural top-down 2D dungeon generator for Unity. Give it a recipe and a seed; it builds a connected layout of rooms and corridors and renders it to Unity Tilemaps. Same recipe and seed always give the same dungeon.

> Working name. Early development — see [Status](#status).

## Who it's for

Unity developers making top-down 2D games (roguelikes, dungeon crawlers, action RPGs) who want generated levels built from their own tiles and hand-designed rooms, rather than fully random noise.

## Features

Working now:

- **Deterministic generation** — seeded PCG32 RNG with per-stage sub-seeds, so changing one stage doesn't reshuffle the others.
- **Flow graph** — main path plus branches, built from a `DungeonRecipe`.
- **Layout solver** — places room templates by matching door sockets, with no overlaps and a retry budget. Fails with a reason instead of throwing or hanging.
- **A\* corridors** — turn penalty, configurable width, never cut through rooms.
- **Connectivity validation** — flood fill checks every room is reachable from the start.
- **Debug view** — draws the generated grid in the Scene view and the inspector.
- **Tilemap rendering** (Phase 2, [PR #3](https://github.com/Chrisd313/dungeon-generator/pull/3)) — `Tileset` asset, top-down wall autotiling, weighted floor variants, wall colliders.

Planned: flow rules (e.g. "boss room last"), a room template editor window, spawn points with events, a preview window and bake-to-scene. See [Roadmap](#roadmap).

## Built with

- Unity 6 (`6000.2.6f2`), C#
- `com.unity.2d.tilemap`
- Unity Test Framework (EditMode tests)

Generation logic lives in `Runtime/Core`. It doesn't use MonoBehaviours, scene access, `UnityEngine.Random` or Tilemap calls, so it is fast and unit-testable.

## Getting started

1. Clone the repo and open the folder in Unity Hub with Unity `6000.2.6f2` (other Unity 6 versions will probably work).
2. Run **Tools → DungeonGen → Create Debug Dungeon In Scene**.
3. Select the created object and press **Generate** in the inspector. Change the seed to get a different dungeon.

Other tools:

- **Tools → DungeonGen → Stress Test Selected Recipe** — generates many seeds from the selected recipe and reports success rate, overlaps and connectivity.
- **Assets → Create → DungeonGen** — create a Dungeon Recipe, Room Template, Room Template Library or Tileset.

## Running tests

In the Editor: **Window → General → Test Runner → EditMode → Run All**.

Headless (close the project in the Editor first):

```bash
./run-tests.sh                 # all EditMode tests
./run-tests.sh LayoutStress    # filter by name
```

The script assumes Unity is installed at `C:/Program Files/Unity/Hub/Editor/6000.2.6f2`. Set `UNITY` to point it at a different install.

Tests cover RNG determinism, the grid, flow graph building, A* pathfinding, wall roles, and a 1,000-seed layout stress test (no overlaps, 100% connectivity).

## Project layout

```
Assets/DungeonGen/
  Runtime/Core/    pure generation: RNG, flow, layout, corridors, grid, tiles
  Runtime/Unity/   MonoBehaviours, rendering, debug view
  Editor/          menus, inspectors, scene setup
  Tests/EditMode/  unit and stress tests
```

## Status

| Phase | Scope | State |
|---|---|---|
| 0 | Skeleton, seeded RNG, `DungeonGrid` | Done |
| 1 | Layout solver, A* corridors, validation, debug view | Done |
| 2 | Tileset, wall autotiling, Tilemap rendering | In review ([PR #3](https://github.com/Chrisd313/dungeon-generator/pull/3)) |
| 3 | Flow rules + recipe | Next |
| 4 | Room Template Editor window | Planned |
| 5 | Spawns + events | Planned |
| 6 | Builder component, preview window, bake to scene | Planned |
| 7 | Demo scene, docs, samples | Planned |

## Roadmap

Full design, data model and acceptance criteria for each phase are in [PLAN.md](PLAN.md).

## License

[MIT](LICENSE)
