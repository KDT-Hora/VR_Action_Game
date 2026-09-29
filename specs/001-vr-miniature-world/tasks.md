---
description: "Task list for VR現実空間連動型・小世界アクションゲーム"
---

# Tasks: VR現実空間連動型・小世界アクションゲーム

**Input**: `/specs/001-vr-miniature-world/` (plan.md, spec.md, research.md, data-model.md, contracts/)

**Tests**: Core(純C#)のテストは必須(決定論・到達可能性、plan.mdの自主原則)。Unity側はスモークテスト中心。

**方針**: スキャンは当面 **FixtureScanProvider(固定の疑似スキャンデータ)** を使う。実スキャン(Meta MRUK)は最後のフェーズで差し替える。

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [ ] T001 Create repo layout: `Core/`, `Game/` (Unity 6 project), `Core/Tests/` per plan.md
- [ ] T002 Initialize `Core/Core.csproj` (netstandard2.1, no UnityEngine reference) and `Core/Tests/Core.Tests.csproj` (NUnit)
- [ ] T003 Initialize Unity 6 project in `Game/` with OpenXR + XR Interaction Toolkit; add asmdef `Core.asmdef` referencing `Core/` sources
- [ ] T004 [P] Add `.gitignore` (Unity Library/Temp/Logs, bin/obj) at repo root
- [ ] T005 [P] Add `README.md` at repo root with build/test commands from quickstart.md

## Phase 2: Foundational (blocks all stories)

- [ ] T006 [P] Implement deterministic PRNG (`SplitMix64`/xoshiro, `Fork(label)`) in `Core/Rng/DeterministicRng.cs` per contracts/interfaces.md `IDeterministicRng`
- [ ] T007 [P] Define data types `ScanResult`, `SpatialData`, `PlaySpace`, `GenerationRequest`, `Stage`, `StageElement` in `Core/Model/*.cs` per data-model.md (cell kinds: Ground|Wall|Obstacle|Platform|Void)
- [ ] T008 [P] Define interfaces `ISpatialAbstractor`, `IStageGenerator`, `IStageValidator` in `Core/Interfaces.cs` per contracts/interfaces.md
- [ ] T009 [P] Test: PRNG produces identical sequences for same seed (100 runs) in `Core/Tests/RngTests.cs`
- [ ] T010 Create fixture scan data (a desk, a room with sofa/shelf/step) as JSON in `Game/Assets/StreamingAssets/Fixtures/` and `Core/Tests/Fixtures/`
- [ ] T011 Implement `IScanProvider` + `FixtureScanProvider` in `Game/Assets/Scripts/Scan/`
- [ ] T012 Implement SaveFile/ShareFile JSON IO (versioned, hash check) in `Core/Serialization/` per contracts/file-formats.md

**Checkpoint**: Core builds, PRNG tests pass, fixtures load.

## Phase 3: User Story 1 — 小さなキャラクターを机の上で操作する (P1) MVP

**Independent Test**: 固定ステージ上で移動・ジャンプができ、覗き込める。

- [ ] T013 [US1] Create scene `Game/Assets/Scenes/Sandbox.unity` with XR rig and a flat fixed floor at desk height
- [ ] T014 [P] [US1] Implement world scale setting (character 5–20cm, default 10cm) in `Game/Assets/Scripts/Presentation/ScaleSettings.cs`
- [ ] T015 [P] [US1] Create the sword-child character prefab/model placeholder in `Game/Assets/Scripts/Character/CharacterView.cs`
- [ ] T016 [US1] Implement controller movement + jump (stick/buttons) in `Game/Assets/Scripts/Character/CharacterController.cs` (FR-002, FR-018)
- [ ] T017 [US1] Implement head-tracked peeking view (player head stays real-scale) in `Game/Assets/Scripts/Presentation/PeekRig.cs` (FR-003)
- [ ] T018 [US1] Support seated and standing play (height offset / recenter) in `Game/Assets/Scripts/Presentation/PostureSetup.cs` (FR-021)
- [ ] T019 [US1] PlayMode smoke test: character moves/jumps in `Game/Assets/Tests/PlayMode/CharacterSmokeTests.cs`

## Phase 4: User Story 2 — 机上ステージを生成して遊ぶ (P2)

**Independent Test**: 疑似机スキャンからステージが生成され、ゴールまで到達できる。同条件で同一。

- [ ] T020 [P] [US2] Test: same spatial data + seed → identical Stage (100 runs) in `Core/Tests/DeterminismTests.cs`
- [ ] T021 [P] [US2] Test: generated stage start→goal reachable across 200 seeds in `Core/Tests/ReachabilityTests.cs`
- [ ] T022 [US2] Implement `SpatialAbstractor` (ScanResult → integer cell grid, regions, links; cell-size quantization) in `Core/SpatialAbstraction/SpatialAbstractor.cs`
- [ ] T023 [US2] Implement Tabletop play-area detection from Table surface in `Core/SpatialAbstraction/TabletopPlaySpace.cs`
- [ ] T024 [US2] Implement exploration-stage generator (regions, start/goal at farthest reachable cells, BFS validation, deterministic retry) in `Core/StageGeneration/ExplorationGenerator.cs` (FR-007, FR-009, FR-010)
- [ ] T025 [US2] Implement `StageValidator` (reachability, minimum area) in `Core/StageGeneration/StageValidator.cs`
- [ ] T026 [US2] Render generated stage as miniature (floor/wall/platform meshes) in `Game/Assets/Scripts/Presentation/StageRenderer.cs`
- [ ] T027 [US2] Goal trigger + clear UI in `Game/Assets/Scripts/Game/StageFlow.cs` (FR-013)
- [ ] T028 [US2] Scene `Game/Assets/Scenes/Tabletop.unity` wiring FixtureScanProvider → Abstractor → Generator → Renderer
- [ ] T029 [P] [US2] Scan-failure handling (too small/empty space → message, retry) in `Game/Assets/Scripts/Scan/ScanErrors.cs`

## Phase 5: User Story 3 — 部屋全体をスキャンして生成 (P3)

**Independent Test**: 疑似部屋データから、家具が別の意味を持つステージが生成される。

- [ ] T030 [P] [US3] Test: room fixture → all walkable regions connected or bridged in `Core/Tests/RoomAbstractionTests.cs`
- [ ] T031 [US3] Extend abstractor for Floor/Wall/Ceiling/Furniture/Step classification into cell kinds in `Core/SpatialAbstraction/RoomAbstractor.cs`
- [ ] T032 [US3] Implement semantic mapping rules (table→Platform, shelf→Wall/ruin, sofa→mountain Obstacle, step→cliff) in `Core/SpatialAbstraction/SemanticMapper.cs` (FR-011)
- [ ] T033 [US3] Room-mode generator support (multi-level regions, links) in `Core/StageGeneration/ExplorationGenerator.cs`
- [ ] T034 [US3] Scene `Game/Assets/Scenes/Room.unity` and mode selection UI (Tabletop/Room) in `Game/Assets/Scripts/Game/ModeSelect.cs` (FR-006)

## Phase 6: User Story 4 — 敵・アイテム・ギミックで遊ぶ (P4)

**Independent Test**: 敵とギミックのあるステージで攻撃・回避・操作しクリアできる。

- [ ] T035 [P] [US4] Test: enemy/item/gimmick placement never blocks the start→goal path in `Core/Tests/PlacementTests.cs`
- [ ] T036 [US4] Rule-based placement (enemies, items, chests, gimmicks, checkpoints) in `Core/StageGeneration/ElementPlacer.cs` (FR-007)
- [ ] T037 [P] [US4] Sword attack + dodge in `Game/Assets/Scripts/Character/CombatController.cs`
- [ ] T038 [P] [US4] Enemy behavior (simple state machine, no AI/ML) in `Game/Assets/Scripts/Game/EnemyController.cs`
- [ ] T039 [US4] HP system + difficulty modes (CheckpointRespawn / NoDeath fall-return) in `Game/Assets/Scripts/Character/HealthSystem.cs` (FR-016)
- [ ] T040 [US4] Pre-stage difficulty selection UI in `Game/Assets/Scripts/Game/DifficultySelect.cs` (FR-026)
- [ ] T041 [US4] Next-stage choice after clear in `Game/Assets/Scripts/Game/NextStageMenu.cs` (FR-024)

## Phase 7: User Story 5 — 複数のステージ種類 (P5)

**Independent Test**: 同一空間から各ステージ種類が生成され、いずれもクリア可能。

- [ ] T042 [P] [US5] Test: each stage type generates a clearable stage from the same spatial data in `Core/Tests/StageTypeTests.cs`
- [ ] T043 [US5] Add Treasure, Combat, Defense, Boss placement rules in `Core/StageGeneration/StageTypes/*.cs` (FR-012)
- [ ] T044 [US5] Stage type selection in `Game/Assets/Scripts/Game/NextStageMenu.cs`

## Phase 8: Save, Share, Safety

- [ ] T045 [P] Save/load spatial data + progress + settings in `Game/Assets/Scripts/Save/SaveStore.cs` (FR-019)
- [ ] T046 [P] Re-scan / discard saved scan in `Game/Assets/Scripts/Save/RescanFlow.cs` (FR-028)
- [ ] T047 [P] Share export/import with hash validation in `Game/Assets/Scripts/Save/ShareService.cs` (FR-025)
- [ ] T048 [P] Test: ShareFile round-trip reproduces identical Stage in `Core/Tests/ShareRoundTripTests.cs`
- [ ] T049 Safety monitor: boundary proximity → auto passthrough, manual toggle in `Game/Assets/Scripts/Presentation/SafetyMonitor.cs` (FR-014, FR-020, FR-027)

## Phase 9: Real scan (later, needs headset)

- [ ] T050 Spike: verify Meta MRUK scene data over PC link on target headset; record result in `specs/001-vr-miniature-world/research.md`
- [ ] T051 Implement `MetaSceneScanProvider` in `Game/Assets/Scripts/Scan/MetaSceneScanProvider.cs` (fallback: `ManualTableScanProvider`)

## Phase 10: Polish

- [ ] T052 [P] Profile and hold 90fps in Room/Tabletop scenes (FR-029)
- [ ] T053 [P] Verify scan→generate ≤ 60s (SC-005) with fixture and real scan
- [ ] T054 Run quickstart.md end-to-end and update it

## Dependencies

- Phase 1 → 2 → US1; US2 depends on Phase 2 (and US1 for playing); US3 depends on US2; US4 depends on US2; US5 depends on US4; Phase 8 depends on US2; Phase 9 last.
- Within a phase, tasks marked [P] run in parallel; tests before their implementation.

## Parallel examples

- Phase 2: T006, T007, T008, T009 together
- US2: T020, T021 together, then T022–T025

## Strategy

MVP = Phase 1–3 (US1)、次に US2 でスキャン→生成→クリアの縦串を通す。実スキャンは Phase 9 で差し替え。
