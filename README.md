# Procedural Stealth-Action Roguelite — Feature Prototype

**University of London — CM3070 Final Project (Template 6.2: Procedural Dungeon Generation in Roguelike Games)**

A single-player, 2D, top-down **stealth-action roguelite** built in **Unity 6.3 LTS (C#)**. The
player infiltrates a procedurally generated facility, evades GOAP-driven guards, completes a
mission objective, and reaches an exit.

This repository is the **feature prototype** described in the preliminary report. It prioritises
three core systems over visual polish (it uses placeholder *Scribble Dungeon* tiles):

1. An end-to-end **procedural content generation (PCG) pipeline**.
2. **Guard AI** using Goal-Oriented Action Planning (GOAP) over A* pathfinding.
3. A functional **gameplay loop**: evade -> complete -> exit.

---

## Playable Build

A WebGL build is available on Itch.io (password: `finalproject`):
<https://asdasdasduu.itch.io/cm3070-final-project-roguelike-prototype>

---

## Signposting for Markers

The most interesting scripts are listed below, grouped by the three prototype goals. Paths are
relative to [`Assets/Scripts`](Assets/Scripts). The codebase is heavily commented.

### Goal 1 — Procedural Content Generation Pipeline

The pipeline runs in four sequential stages, orchestrated by `FacilityOrchestrator`:

| Stage             | Script | What it does                                                                                                                                                  |
|-------------------|--------|---------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 0 - Orchestration | [`Generation/FacilityOrchestrator.cs`](Assets/Scripts/Generation/FacilityOrchestrator.cs) | Runs the pipeline in order and wires the spawners together. **Best entry point for reading the system.**                                                      |
| 1 - Mission       | [`Generation/MissionGenerator.cs`](Assets/Scripts/Generation/MissionGenerator.cs) | Seeded RNG selects a mission type (Assassination / Theft / Sabotage) and builds a directed acyclic `MissionGraph` of objective nodes with dependencies.       |
| 2 - Rooms         | [`Rooms/RoomGraphGenerator.cs`](Assets/Scripts/Rooms/RoomGraphGenerator.cs) | Maps mission nodes to rooms with roles (Objective, Keycard, Guard Post, Exit…), turns dependency edges into locked doors, and adds exits based on difficulty. |
| 3 - Tiles         | [`Generation/TiledLayoutGenerator.cs`](Assets/Scripts/Generation/TiledLayoutGenerator.cs) | Lays the room graph onto a 2D tile grid — a "spine" to the primary objective with branching subtrees — then paints walls/floors and carves doorways.          |
| 4 - Population    | The spawners (below) | Instantiate the player, keycards, objectives, guards, exits, distractions, and cover.                                                                         |

Supporting scripts:

- [`Generation/LevelOrchestrator.cs`](Assets/Scripts/Generation/LevelOrchestrator.cs) — level progression and **difficulty/complexity scaling** (raised every 10 levels).
- Population spawners: [`Player/PlayerSpawner.cs`](Assets/Scripts/Player/PlayerSpawner.cs), [`Keycards/KeycardSpawner.cs`](Assets/Scripts/Keycards/KeycardSpawner.cs), [`Doors/LockedDoorSpawner.cs`](Assets/Scripts/Doors/LockedDoorSpawner.cs), [`Objectives/ObjectiveSpawner.cs`](Assets/Scripts/Objectives/ObjectiveSpawner.cs), [`Exits/ExitSpawner.cs`](Assets/Scripts/Exits/ExitSpawner.cs), [`Guards/GuardSpanwer.cs`](Assets/Scripts/Guards/GuardSpanwer.cs), [`Items/DistractionSpawner.cs`](Assets/Scripts/Items/DistractionSpawner.cs), [`Items/CoverSpawner.cs`](Assets/Scripts/Items/CoverSpawner.cs).
- [`Editor/MissionGraphEditorWindow.cs`](Assets/Editor/MissionGraphEditorWindow.cs) — a **custom Unity Editor tool** that visualises the `MissionGenerator` and `RoomGraphGenerator` output as inspectable graphs.

### Goal 2 — Guard AI (GOAP + A*)

- [`Guards/GOAP/GuardAgent.cs`](Assets/Scripts/Guards/GOAP/GuardAgent.cs) — the guard "brain": each frame it picks the highest-priority goal (Chase -> Investigate -> Distraction -> Patrol), plans, and executes.
- [`Guards/GOAP/GuardPlanner.cs`](Assets/Scripts/Guards/GOAP/GuardPlanner.cs) — the **GOAP planner**: breadth-first search over action preconditions/effects to find the shortest valid plan.
- [`Guards/GOAP/GuardActions.cs`](Assets/Scripts/Guards/GOAP/GuardActions.cs), [`GuardGoals.cs`](Assets/Scripts/Guards/GOAP/GuardGoals.cs), [`GuardState.cs`](Assets/Scripts/Guards/GOAP/GuardState.cs) — the action set, goal definitions, and the fixed-size world state.
- [`Navigation/AStarPathfinder.cs`](Assets/Scripts/Navigation/AStarPathfinder.cs) — A* over the generated tile grid (4-connected, Manhattan heuristic, binary min-heap frontier).
- [`Guards/GuardMovement.cs`](Assets/Scripts/Guards/GuardMovement.cs) — follows A* paths, but switches to direct homing (`SeekDirect`) when the guard has direct line of sight to an exposed player.
- [`Guards/GuardVision.cs`](Assets/Scripts/Guards/GuardVision.cs) — line-of-sight and detection-cone checks that feed the agent's world state.
- [`Guards/WaypointDeriver.cs`](Assets/Scripts/Guards/WaypointDeriver.cs) — derives patrol routes from the room graph.
- [`Navigation/NavigationDebugDrawer.cs`](Assets/Scripts/Navigation/NavigationDebugDrawer.cs) — the debug visualisation of A* paths.

### Goal 3 — Gameplay Loop

- [`Player/PlayerController.cs`](Assets/Scripts/Player/PlayerController.cs) — WASD movement and input.
- [`Player/PlayerHiding.cs`](Assets/Scripts/Player/PlayerHiding.cs) — cover mechanic (invisible to guards that aren't actively chasing).
- [`Player/PlayerDistractionInventory.cs`](Assets/Scripts/Player/PlayerDistractionInventory.cs) + [`Items/DistractionItem.cs`](Assets/Scripts/Items/DistractionItem.cs) — pick up and drop distraction items that divert guards.
- [`Objectives/ObjectiveMinigame.cs`](Assets/Scripts/Objectives/ObjectiveMinigame.cs) — the **quick-time event (QTE)** triggered at objectives, completed while stationary.
- [`Objectives/ObjectiveTracker.cs`](Assets/Scripts/Objectives/ObjectiveTracker.cs) + [`Objectives/ObjectiveHUD.cs`](Assets/Scripts/Objectives/ObjectiveHUD.cs) — objective state and HUD.
- [`Keycards/Keycard.cs`](Assets/Scripts/Keycards/Keycard.cs) + [`Doors/LockedDoor.cs`](Assets/Scripts/Doors/LockedDoor.cs) — the lock-and-key access system.

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Generation/   # PCG pipeline orchestration, mission + tile generation, level progression
│   ├── Rooms/        # Room-connectivity graph and room colour coding
│   ├── Navigation/   # A* pathfinding and facility navigation
│   ├── Guards/       # Guard movement, vision, waypoints; GOAP/ subfolder holds the planner
│   ├── Objectives/   # Objectives, QTE minigame, tracker, HUD
│   ├── Player/       # Controller, hiding, inventories, health, HUDs
│   ├── Items/        # Distraction and cover items + spawners
│   ├── Keycards/ Doors/ Exits/ Disguises/   # Access systems and run flow
│   ├── Menu/ Camera/ Audio/                 # UI, camera follow, audio
│   └── ...
└── Editor/           # MissionGraphEditorWindow — custom graph visualiser tool
```

## Tech & Tooling

- **Engine:** Unity 6.3 LTS — **Language:** C#
- **Version control:** Git + Git LFS (see [`.gitattributes`](.gitattributes))
- **Target:** Windows 11 (60 fps) with a WebGL build (30 fps) for remote testing

---

## Asset Attribution

### Git Ignore for Unity
- Pre-made `.gitignore` for Unity projects.
- <https://github.com/github/gitignore/blob/main/Unity.gitignore>
- Licence: Creative Commons (CC0 1.0), <https://github.com/github/gitignore/blob/main/LICENSE>

### Git Attributes (LFS) for Unity
- Pre-made `.gitattributes` for Unity.
- <https://github.com/FrankNine/RepoConfig/blob/master/.gitattributes>
- Licence: MIT, <https://github.com/FrankNine/RepoConfig?tab=MIT>
