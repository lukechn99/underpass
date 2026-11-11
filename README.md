# Underpass

A stylized 3D, top-down robot exploration game: descend through a vast underground
world, combine discovered weapons, read enemy attacks, and choose when to fight
or escape. Native iOS is the primary target; phone and desktop browsers provide
quick play. Single-player first, with future co-op considered in the architecture.

## Current status

This is the **initial source foundation and whitebox technical trial**, not a
finished or performance-validated game. The shared C# rules pass **15 scenario
checks**. Unity Build Automation build 2 (`cea8006`) was run in desktop Chrome:
the environment renders, the robot remains grounded, movement and inventory
respond, and the previous collider/orientation errors are absent.

Build 3 (`d872917`) verifies versioned local saves in desktop Chrome: an equipment
change survives closing/reopening the game tab; leaving an unfinished fight by
closing the tab restores the prior safe room, entry health and loadout; a damaged
primary save recovers from backup. All 15 core checks also pass in GitHub Actions.
Native iOS, phone input, full progression and sustained performance remain
unverified. No 60-fps claim is made.

- [Design decisions and open questions](docs/DESIGN.md)
- [Cloud build and phone testing setup](docs/CLOUD_BUILDS.md)
- [Implementation status and next steps](docs/IMPLEMENTATION.md)

## Project layout

| Path | Purpose |
|---|---|
| `Assets/Scripts/Core` | Engine-independent progression, equipment and checkpoint rules |
| `Assets/Scripts/Prototype` | Replaceable Unity whitebox presentation and input |
| `Assets/Editor/TrialBuild.cs` | Editor/cloud build preparation and export entry points |
| `Assets/Scenes/Trial.unity` | Trial entry scene |
| `Tests/Core` | Scenario tests compiled against the **same** core source files |

The project is at the repository root. Unity is pinned to **6000.3.28f1 (6.3 LTS)**
in `ProjectSettings/ProjectVersion.txt`. The first bootstrap uses the built-in
renderer and simple runtime geometry; the production renderer remains open for
the lighting/performance trial. Both robot concept candidates remain open.

## Run the core checks

Install .NET SDK 8, then:

```sh
dotnet run --project Tests/Core/Underpass.Core.Checks.csproj --configuration Release
```

The GitHub Actions `Core rules` workflow runs this command. It is not a Unity
compilation or rendering test.

## Open or build the Unity trial

Open the repository folder with the pinned editor. Select **Underpass → Prepare
trial**, open `Assets/Scenes/Trial.unity`, and press Play. Preparation generates
the small material resources needed by the whitebox scene. It also runs before
a build, including builds initiated by a build service.

For a configured, licensed editor with Web Build Support:

```sh
Unity -batchmode -quit -projectPath "$PWD" -executeMethod Underpass.Editor.TrialBuild.Web -logFile Build-Web.log
```

The result is `Build/Web/index.html` and its accompanying files; serve the whole
folder over HTTP(S). The analogous `Underpass.Editor.TrialBuild.IOS` entry point
exports an Xcode project to `Build/iOS` on a suitably configured builder. It does
not sign the app or upload it to TestFlight.

### Whitebox controls

- **WASD:** move; **mouse/trackpad:** look; **hold primary click:** fire all mounts.
- **Touch:** left lower region is the movement stick; right lower region aims
  and fires; drag elsewhere to look without firing.
- **I / Inventory:** equipment controls, slowing the world to 20% (provisional).
- **E / Collect:** collect nearby crate equipment; shooting a crate also collects.
- **Tab / Map:** debug room diagram, not the finished discovery/terrain map.
- **F9:** deliberately trigger defeat to exercise recovery rules.
- **Recover:** heal at a prototype safe place.
- Walk onto the black descent marker in room D to enter the second layer.

The UI, creature meshes, attacks, robot shape and room art are placeholders.
Progress is saved to platform-local PlayerPrefs storage (IndexedDB on the web).
An unfinished room resumes at the last safe room with room-entry health and
inventory; completed rooms, checkpoints and shared storage persist. Saves are
checked every second and at conquest, defeat, inventory toggles and app lifecycle
callbacks. A backup is kept; unreadable saves are preserved while a temporary
session runs. Private browsing, site-data deletion and platform storage eviction
can remove local progress. Browser/native storage behaviour still needs device
validation. Device benchmarks and final gameplay visibility are not implemented.
