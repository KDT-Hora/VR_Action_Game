# VR Action Game (小世界アクション)

Scan a desk or room, generate a small playable world from it with deterministic rules (no AI), and control a small sword-wielding child while peering into that world in VR.

Spec, plan and tasks: `specs/001-vr-miniature-world/`.

## Layout
- `Core/` — Unity-independent C# (netstandard2.1): spatial abstraction, deterministic stage generation, character rules, safety, JSON save/share. Also a UPM local package (`com.kdt.vraction.core`).
- `Game/` — Unity 6 project (6000.2.11f1): scan providers, rendering, input, scenes (`Sandbox`, `Tabletop`, `Room`).

## Tests
```
# Core logic (fast, no Unity needed)
cd Core/Tests && dotnet test

# Unity PlayMode / EditMode (batch mode)
"<Unity>/Editor/Unity.exe" -batchmode -nographics -projectPath Game -runTests -testPlatform PlayMode -testResults results.xml
"<Unity>/Editor/Unity.exe" -batchmode -nographics -projectPath Game -runTests -testPlatform EditMode -testResults results.xml
```
Scenes are (re)created with menu `VrAction > Create Scenes`.

## Controls
Left stick / WASD = move, A / Space = jump, right trigger / J = attack, B / K = dodge. Menu: stick left/right (or arrow keys) + A / Enter.

## Status
Scanning uses fixed fixture data (`Core/Fixtures/ScanFixtures.cs`) until a headset scan provider is implemented (tasks T067–T069).
