# Top-Down Dungeon Generator — Build Plan

> Working name: **DungeonGen** (placeholder — rename before release).
> Target: Unity Asset Store tool that procedurally generates full top-down 2D dungeons (rooms + corridors) at runtime or edit time, from user-supplied tiles and designer-authored room templates.

---

## 1. Goals

- Designers author **room templates** in a custom editor window and describe dungeon structure with **flow rules** (e.g. "boss room last, shop between rooms 4–6").
- The generator produces a connected dungeon from a **recipe + seed**, renders it to Unity Tilemaps, and reports **spawn points** via events so the user's own code controls instantiation.
- Generation is **deterministic**: same recipe + seed = identical dungeon, always.
- Ship a polished demo, docs and samples — those sell the asset.

## 2. Scope

### v1 (this plan)
- Top-down only, rectangular grid
- Dungeon Recipe asset with flow rules (main path, branches, loops, placement rules, fill rule)
- Graph-guided spatial layout with door-socket matching, retry budget and graceful failure
- A* corridors with turn penalty and configurable width
- Room templates authored in a custom editor window (floor, wall, door sockets, spawn zones, tags, weights, depth conditions)
- Tileset asset with built-in top-down wall autotiling and weighted floor variants
- Spawn tables with weights, count ranges and distance rules
- C# events + UnityEvents for generation and spawn points; optional default spawner
- Dungeon preview window (seed, regenerate, randomise) and Bake to Scene
- Demo scene, quick-start docs, samples

### Explicitly out of v1
Isometric, visual flow-graph editor, procedural room shapes (caves/stamps), template rotation/mirroring, lock-and-key, multi-floor, corridor templates, WFC.

## 3. Technical constraints

- **Unity version:** minimum 2022.3 LTS (confirm — see Open Questions). Depends on `com.unity.2d.tilemap`.
- **Layout:** package-style folder with separate assembly definitions:
  ```
  Assets/DungeonGen/
    Runtime/
      Core/        # pure generation logic
      Unity/       # Tilemap rendering, MonoBehaviours, ScriptableObjects
    Editor/        # editor windows, drawers, inspectors
    Tests/
      EditMode/
    Samples~/      # demo scene, sample tileset, sample templates
    Documentation~/
  ```
- **Namespace:** `DungeonGen` (+ `.Core`, `.Editor`, etc.).
- **Core is engine-light:** `Runtime/Core` may use Unity math types (`Vector2Int`, `RectInt`) but **no** MonoBehaviours, scene access, `UnityEngine.Random`, or Tilemap calls. This keeps it fast, unit-testable, and safe to run off the main thread later.
- **Randomness:** never use `UnityEngine.Random`. Use an `IRandom` wrapper around a seeded PRNG. Derive **sub-seeds per stage** (flow, layout, corridors, decoration, spawns) from the master seed, so tweaking one stage doesn't reshuffle the others.
- No allocations in hot loops where avoidable; generation for a ~20-room dungeon should typically complete in well under 100 ms (measure, then tighten).

## 4. Pipeline

```
DungeonRecipe + seed
   │
   ▼
1. Flow        → FlowGraph (abstract room slots + edges, each slot tagged)
2. Assignment  → each FlowNode gets a RoomTemplate (by tag, weight, depth)
3. Layout      → PlacedRooms positioned on grid via door sockets, no overlaps
4. Corridors   → A* paths carved between connected sockets
5. Grid build  → DungeonGrid (floor/wall/empty + room ids), walls derived
6. Validation  → flood fill: everything reachable from start
7. Spawns      → SpawnPoints from spawn zones + spawn tables + rules
   │
   ▼
DungeonResult ──► DungeonRenderer (Tilemaps)
              └─► events: OnGenerated, OnSpawnPoint
```

Each stage is a separate class with a single entry method, taking its inputs and an `IRandom`, returning its output. Stages can fail; failure returns a reason (never throws, never hangs).

## 5. Data model

### Authoring assets (ScriptableObjects)

**`DungeonRecipe`**
- `IntRange mainPathLength` (e.g. 8–12, includes start and end)
- `IntRange branchCount`, `IntRange branchLength`
- `IntRange loopCount` (best-effort)
- `[SerializeReference] List<FlowRule> flowRules`
- `string defaultFillTag` (e.g. `"combat"`)
- `RoomTemplateLibrary templates`
- `Tileset tileset`
- `CorridorSettings corridors`
- `List<SpawnTable> spawnTables`
- `int roomSpacing` (minimum empty gap between rooms, leaves space for corridors)
- `int maxAttempts` (whole-dungeon retries before reporting failure)

**`FlowRule`** (abstract, `[Serializable]`, polymorphic via `[SerializeReference]` + custom drawer)
- `int priority`
- `abstract bool Apply(FlowGraph graph, IRandom rng)`
- Subclasses for v1:
  - `FixedPositionRule` — `tag`, position `Start | End | Index(n)`
  - `RangeRule` — `tag`, `minIndex`, `maxIndex`, `count` (main path only)
  - `BranchEndRule` — `tag`, applies to branch tips
  - `NoConsecutiveRule` — `tag`, forbids two adjacent nodes with that tag
- Apply order: priority, then Fixed → Range → BranchEnd → constraint rules. Unfilled nodes get `defaultFillTag`.

**`RoomTemplate`**
- `Vector2Int size`
- `CellType[] cells` (flattened `size.x * size.y`; `CellType = Empty | Floor | Wall`)
- `List<DoorSocket> sockets`
- `List<SpawnZone> spawnZones`
- `List<string> tags`
- `float weight`
- `int minDepth`, `int maxDepth` (−1 = no limit)
- `bool allowFloorVariation`

**`DoorSocket`** (`[Serializable]`)
- `Vector2Int position` (on the room's edge)
- `Direction facing` (`North | East | South | West`)
- `int width`

**`SpawnZone`** (`[Serializable]`)
- `string id`
- `List<Vector2Int> cells`
- `string spawnTableId` (or direct reference)
- `bool isPlayerStart`

**`RoomTemplateLibrary`**
- `List<RoomTemplate> templates`
- Query: `Pick(tag, depth, IRandom)` → weighted random among matching.

**`Tileset`**
- `List<WeightedTile> floorTiles`
- Wall tiles resolved by neighbour bitmask: a lookup from 8-neighbour mask → `TileBase`, with sensible fallbacks so a minimal tileset (top, side, corners) still works. Provide a custom inspector showing which roles are filled.
- `TileBase doorTile` (optional), `List<WeightedTile> decorTiles` (optional)

**`CorridorSettings`**
- `int width`
- `float turnPenalty`
- `int maxLength`
- `bool allowMerging` (reusing existing corridor cells is cheaper)

**`SpawnTable`**
- `string id`
- `List<SpawnEntry> entries`, each with:
  - `string key` (what the user's code maps to a prefab/pool)
  - `GameObject prefab` (optional, used only by the default spawner)
  - `float weight`
  - `IntRange count`
  - `int minDistanceFromPlayer`
  - `int minDistanceFromDoor`

### Runtime data (plain C#, in Core)

**`FlowGraph`** — `List<FlowNode> nodes`, `List<FlowEdge> edges`
**`FlowNode`** — `int id`, `string tag`, `int depth`, `bool isMainPath`, `int parentId`
**`FlowEdge`** — `int from`, `int to`, `bool isLoop`

**`PlacedRoom`** — `int id`, `FlowNode node`, `RoomTemplate template`, `Vector2Int origin`, `RectInt bounds`
**`Corridor`** — `int fromRoom`, `int toRoom`, `List<Vector2Int> path`

**`DungeonGrid`** — dense arrays over a bounding `RectInt`: `CellType` per cell, `int roomId` per cell (−1 = corridor/none), plus helpers (`InBounds`, `Get`, `Set`, neighbour queries).

**`SpawnPoint`** — `string key`, `Vector2Int cell`, `int roomId`, `string zoneId`

**`DungeonResult`**
- `bool success`, `string failureReason`
- `int seed`
- `FlowGraph flow`
- `List<PlacedRoom> rooms`, `List<Corridor> corridors`
- `DungeonGrid grid`
- `Vector2Int playerStart`
- `List<SpawnPoint> spawnPoints`

### Runtime API

```csharp
// Pure generation (Core)
DungeonResult result = DungeonGenerator.Generate(recipe, seed);

// Rendering (Unity layer)
DungeonRenderer.Render(result, recipe.tileset, floorTilemap, wallTilemap);
```

**`DungeonBuilder`** (MonoBehaviour, the friendly entry point)
- Fields: `recipe`, `seed`, `useRandomSeed`, `floorTilemap`, `wallTilemap`, `generateOnStart`, `useDefaultSpawner`
- `DungeonResult Build()` / `Build(int seed)` / `Clear()`
- Events: C# `event Action<DungeonResult> Generated`, `event Action<SpawnPoint> SpawnPointCreated`, plus matching `UnityEvent`s for inspector wiring.

## 6. Algorithm notes

**Flow**
1. Build main path chain of length N (start = depth 0).
2. Attach branches to random main-path nodes (never start or end), each a chain of `branchLength`.
3. Add loop edges between nodes of similar depth that aren't already adjacent (best-effort; dropped later if unroutable).
4. Apply flow rules; fill remaining nodes with the default tag.
5. If a rule can't be satisfied (e.g. range too small), fail with a clear reason that names the rule.

**Assignment** — for each node, `templates.Pick(tag, depth)`. A template must have at least as many sockets as the node's degree; prefer templates whose socket directions suit the layout.

**Layout** (graph-guided incremental placement — the flow graph already defines connections, so no MST step is needed)
1. Place start room at origin.
2. BFS through the graph. For each child, choose a free socket on the parent and a free socket on the child template facing the opposite direction.
3. Position the child so sockets face each other across a gap of `roomSpacing` (randomised within a small range).
4. Check AABB overlap with all placed rooms (with padding). On overlap, try other socket pairs, then other templates for that tag.
5. Limited backtracking (a node budget); if exceeded, restart the whole attempt with the next sub-seed. After `maxAttempts`, return failure.

**Corridors**
- A* on the grid between the cells just outside each paired socket.
- Cost: 1 per step + `turnPenalty` per direction change; room interiors impassable; existing corridor cells cheaper if `allowMerging`.
- Carve with a brush of `width`. Loop edges are routed last and dropped silently if no path is found within `maxLength`.

**Grid build + walls**
- Stamp room cells and corridor cells into `DungeonGrid`.
- Any `Empty` cell 8-adjacent to `Floor` becomes `Wall`.
- Renderer resolves each wall's tile from its neighbour bitmask via the Tileset.

**Validation** — flood fill from player start; all room floor cells must be reached. Failure → retry.

**Spawns**
1. Player start = first `isPlayerStart` zone in the start room (fallback: start room centre floor cell).
2. Compute BFS distance field from player start and from all door cells.
3. Mark **reserved** cells: shortest paths between each room's sockets (so spawns never block a route).
4. For each zone, pick entries from its table by weight/count; choose cells from the zone that pass the distance rules and aren't reserved or already used.
5. Emit `SpawnPoint`s; never instantiate in Core.

## 7. Editor tooling (UI Toolkit; avoid GraphView — still experimental)

**Room Template Editor window**
- Grid canvas with zoom/pan; brushes: Floor, Wall, Erase, Door Socket (click edge cell, set facing/width), Spawn Zone (paint cells into the selected zone).
- Resize controls; side inspector for tags, weight, depth limits, zones list.
- Preview toggle rendering with a chosen Tileset.
- Validation warnings (no sockets, socket not on edge, unreachable floor).

**Dungeon Preview window**
- Recipe field, seed field, Regenerate, Randomise Seed.
- Fast coloured-cell view of the grid (rooms tinted by tag, sockets, spawn points); optional tile render.
- Shows failure reason clearly when generation fails.
- **Bake to Scene**: writes the result into the target Tilemaps so designers can hand-edit.

**Inspectors/drawers**
- `[SerializeReference]` FlowRule list with an "Add Rule ▾" type picker.
- Tileset inspector showing filled/missing wall roles.

## 8. Testing (EditMode, Unity Test Framework)

- **Determinism:** same recipe + seed → identical result hash (rooms, corridors, spawns).
- **Stress:** 1,000 seeds on a sample recipe — 0 overlaps, 100% connectivity, success rate reported (target ≥ 99% within `maxAttempts`).
- **Rules:** each FlowRule subclass has tests for satisfied and unsatisfiable cases.
- **Corridors:** A* finds paths, respects turn penalty, never cuts through rooms.
- **Spawns:** distance rules respected; reserved cells never used.
- **Performance:** timing test for a 20-room dungeon (log, then set a budget).
- Add an editor menu item to run a stress batch and print stats.

## 9. Phases and acceptance criteria

Work strictly phase by phase. Don't start the next phase until the current one's acceptance criteria pass.

**Phase 0 — Skeleton**
- Folder structure, asmdefs, namespaces, `IRandom` + sub-seeding, `DungeonGrid`, test assembly running.
- ✅ Empty test suite runs; `IRandom` determinism test passes.

**Phase 1 — Spike: layout + corridors (highest risk)**
- Hardcoded rectangle templates with sockets, simple linear + branched graph, layout solver, A* corridors, Gizmo debug view.
- ✅ 1,000 seeds × 15 rooms: 0 overlaps, 100% connected, deterministic.

**Phase 2 — Spike: tiles + walls (second risk)**
- `Tileset` asset, wall derivation, bitmask resolver, `DungeonRenderer` to Tilemaps, floor variants.
- ✅ Dungeon renders correctly with a minimal and a full tileset; no missing-tile gaps.

**Phase 3 — Flow rules + recipe**
- `DungeonRecipe`, `FlowGraph` builder, all four rule types, `RoomTemplateLibrary` with tag/weight/depth picking, failure reasons.
- ✅ Rule tests pass; boss-last/shop-in-range recipe holds over 1,000 seeds.

**Phase 4 — Room Template Editor window**
- Full authoring as specified in §7, with validation warnings.
- ✅ A designer can build a template from scratch and use it in generation without touching the inspector.

**Phase 5 — Spawns + events**
- Spawn tables, distance fields, reserved paths, `SpawnPoint` events, default spawner.
- ✅ Spawn tests pass; demo scene spawns enemies/items via events.

**Phase 6 — Builder + preview + bake**
- `DungeonBuilder` component, Dungeon Preview window, Bake to Scene.
- ✅ One-click regenerate in editor; runtime generation from a button in play mode.

**Phase 7 — Release prep**
- Demo scene (playable character, sample tileset, ~10 sample templates, 2 sample recipes).
- Docs: quick start (under 10 minutes to a first dungeon), API reference, FAQ.
- Zero console warnings, no stray `Debug.Log`, all public API XML-documented.
- All sample art is original or properly licensed for redistribution.
- ✅ Fresh project import → quick start works first time.

## 10. Working conventions for Claude Code

- Write Core tests alongside (or before) Core code.
- Keep Core free of scene/engine calls (see §3).
- Small, focused commits per feature; descriptive messages.
- **Ask before changing the data model** in §5 — it's the contract everything else depends on.
- Prefer clarity over cleverness; this code ships to customers who will read it.
- When generation fails, return reasons a designer can act on ("RangeRule 'shop' needs index 4–6 but main path is only 3 long").

## 11. Open questions

- Final product name and namespace.
- Minimum Unity version (2022.3 LTS vs Unity 6 only).
- Template rotation/mirroring — large boost to variety, but complicates sockets; candidate for v1.1.
- Source of the demo tileset (make, commission, or license).
- Pricing and launch positioning.
