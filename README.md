# Underpass

A stylized 3D, top-down robot exploration game: descend through a vast underground
world, combine discovered weapons, read enemy attacks, and choose when to fight
or escape. Native iOS is the primary target; phone and desktop browsers provide
quick play. Single-player first, with future co-op considered in the architecture.

## Current status

This is the **initial source foundation and whitebox technical trial**, not a
finished or performance-validated game. The shared C# rules have passed 11 local
scenario checks. **The Unity scripts and builds have not yet been compiled or
run in Unity**: the current development workspace has no Unity editor/license.
Cloud build configuration is the next validation gate. No 60-fps claim is made.

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

### Whitebox controls (pending Unity validation)

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
The world is currently in-memory; on-device pause restoration is wired, but
durable local save files, full app-restart recovery, device benchmarks and final
gameplay visibility are not implemented yet.
