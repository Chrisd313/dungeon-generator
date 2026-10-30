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
- **Tilemap rendering** — `Tileset` asset, top-down wall autotiling, weighted floor variants, wall colliders.

Planned: flow rules (e.g. "boss room last"), a room template editor window, spawn points with events, a preview window and bake-to-scene. See [Roadmap](#roadmap).

## Built with

- Unity 6, C#
- `com.unity.2d.tilemap`
- Unity Test Framework (EditMode tests)

Generation logic lives in `Runtime/Core`. It doesn't use MonoBehaviours, scene access, `UnityEngine.Random` or Tilemap calls, so it is fast and unit-testable.

## Setup

### Requirements

- Unity `6000.2.6f2`. This is the only version the project has been developed and tested on, and the minimum supported version isn't decided yet.
- No manual package installs: Unity resolves dependencies from `Packages/manifest.json` when the project opens.

### Run the project

This repo is a complete Unity project, so that is the supported way to run it today.

1. Clone the repo and open the folder in Unity Hub with Unity `6000.2.6f2`.
2. Run **Tools → DungeonGen → Create Debug Dungeon In Scene**. This creates a grid with floor and wall tilemaps, generates a placeholder tileset in `Assets/DungeonGen Placeholder/` (ignored by git), renders a first dungeon from built-in rectangle rooms, and selects the object.
3. In the inspector, press **Next Seed** to get a different dungeon, or **Frame Dungeon** to find it in the Scene view. Assign your own recipe in the **Recipe** field to replace the built-in rooms.

### Use it in another project

DungeonGen isn't a Unity package yet: there is no `package.json`, so it can't be installed through the Package Manager or a Git URL, and `Samples~` and `Documentation~` don't exist yet. Packaging is planned for release prep (Phase 7).

For now, copy `Assets/DungeonGen/` into your project's `Assets/` folder and install **2D Tilemap Editor** (`com.unity.2d.tilemap`) in the Package Manager. This route hasn't been tested yet.

### Other tools

- **Tools → DungeonGen → Stress Test Selected Recipe** — generates 1,000 seeds from the selected recipe and logs success rate and timing.
- **Tools → DungeonGen → Create Placeholder Tileset** — generates the placeholder tileset on its own.
- **Assets → Create → DungeonGen** — create a Dungeon Recipe, Room Template, Room Template Library or Tileset.

## Running tests

In the Editor: **Window → General → Test Runner → EditMode → Run All**.

Headless (close the project in the Editor first):

```bash
./run-tests.sh                 # all EditMode tests
./run-tests.sh LayoutStress    # filter by name
```

The script needs a bash shell (Git Bash on Windows) and assumes the default Windows install path, `C:/Program Files/Unity/Hub/Editor/6000.2.6f2`. Set `UNITY` to the Unity executable for any other install.

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
| 2 | Tileset, wall autotiling, Tilemap rendering | Done |
| 3 | Flow rules + recipe | Next |
| 4 | Room Template Editor window | Planned |
| 5 | Spawns + events | Planned |
| 6 | Builder component, preview window, bake to scene | Planned |
| 7 | Demo scene, docs, samples | Planned |

## Roadmap

Full design, data model and acceptance criteria for each phase are in [PLAN.md](PLAN.md).

## License

[MIT](LICENSE)
