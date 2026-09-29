---
description: "Task list for VR現実空間連動型・小世界アクションゲーム"
---

# Tasks: VR現実空間連動型・小世界アクションゲーム

**Input**: `/specs/001-vr-miniature-world/` (plan.md, spec.md, research.md, data-model.md, contracts/)

**TDD (憲章 I, NON-NEGOTIABLE)**: 各実装タスクの直前に `[TEST]` タスクを置く。手順は必ず ①テストを書く ②実行して失敗(Red)を確認 ③実装して成功(Green) ④整理(Refactor)。テストが書けない見た目・入力の作業は、PlayModeスモークテストか手動確認手順(quickstart.md)を先に書く。

**方針**: スキャンは当面 **FixtureScanProvider(固定の疑似スキャンデータ)** を使う。実スキャン(Meta MRUK)は最後のフェーズで差し替える。

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [ ] T001 Create repo layout: `Core/`, `Core/Tests/`, `Game/` (Unity 6 project) per plan.md
- [ ] T002 Initialize `Core/Core.csproj` (netstandard2.1, no UnityEngine reference) and `Core/Tests/Core.Tests.csproj` (NUnit); confirm `dotnet test` runs (0 tests)
- [ ] T003 (FR-017) Initialize Unity 6 project in `Game/` with OpenXR + XR Interaction Toolkit; make `Core/` a UPM local package (`Core/package.json` + `Core.asmdef`) referenced via `Game/Packages/manifest.json` as `file:../../Core`; confirm Unity compiles it
- [ ] T004 [P] Add `.gitignore` (Unity Library/Temp/Logs/obj, bin/obj) at repo root
- [ ] T005 [P] Add `README.md` at repo root with build/test commands from quickstart.md

## Phase 2: Foundational (blocks all stories)

- [ ] T006 [P] [TEST] PRNG: same seed → identical sequence (100 runs), `Fork(label)` is stable, different seeds differ in `Core/Tests/RngTests.cs` (Red)
- [ ] T007 [P] Implement deterministic PRNG (`SplitMix64`/xoshiro, `Fork(label)`) in `Core/Rng/DeterministicRng.cs` per contracts/interfaces.md `IDeterministicRng` (Green)
- [ ] T008 [P] Define data types `ScanResult`, `SpatialData`, `PlaySpace`, `GenerationRequest`, `Stage`, `StageElement` in `Core/Model/*.cs` per data-model.md (cell kinds: Ground|Wall|Obstacle|Platform|Void); define interfaces `ISpatialAbstractor`, `IStageGenerator`, `IStageValidator` in `Core/Interfaces.cs`
- [ ] T009 Create fixture scan data (a desk, a room with sofa/shelf/step) as JSON in `Core/Tests/Fixtures/` (copied to `Game/Assets/StreamingAssets/Fixtures/`)
- [ ] T010 [TEST] SaveFile/ShareFile JSON: round-trip equality, unknown `version` rejected, `spatialDataHash` mismatch rejected in `Core/Tests/SerializationTests.cs` (Red)
- [ ] T011 Implement SaveFile/ShareFile JSON IO (versioned, hash check) in `Core/Serialization/` per contracts/file-formats.md (Green)
- [ ] T012 [TEST] PlayMode smoke: `FixtureScanProvider.ScanAsync` returns the desk and room fixtures in `Game/Assets/Tests/PlayMode/FixtureScanTests.cs` (Red)
- [ ] T013 Implement `IScanProvider` + `FixtureScanProvider` in `Game/Assets/Scripts/Scan/` (Green)

- [ ] T067 Spike (moved up from Phase 9; ID kept): if a headset is available, verify Meta MRUK scene data over PC link and record the result in `specs/001-vr-miniature-world/research.md`; if not, review the published MRUK scene data fields (anchor labels, plane/volume geometry) against `ScanResult` in `Core/Model/` and record any needed schema changes in research.md (FR-004, FR-017)

**Checkpoint**: `dotnet test` green, fixtures load in Unity, `ScanResult` schema reviewed against the real scan source (T067).

## Phase 3: User Story 1 — 小さなキャラクターを机の上で操作する (P1) MVP

**Independent Test**: 固定ステージ上で移動・ジャンプができ、覗き込める。

- [ ] T014 [P] [US1] [TEST] Scale settings: clamps to 5–20cm, default 10cm in `Core/Tests/ScaleSettingsTests.cs` (Red)
- [ ] T015 [P] [US1] Implement scale settings (pure C#, FR-001) in `Core/Model/ScaleSettings.cs` and Unity wrapper `Game/Assets/Scripts/Presentation/ScaleSettingsBehaviour.cs` (Green)
- [ ] T016 [US1] [TEST] Movement/jump logic: input vector → position/velocity, jump only when grounded, gravity, in `Core/Tests/CharacterMotorTests.cs` (Red)
- [ ] T017 [US1] Implement `CharacterMotor` (pure C#) in `Core/Character/CharacterMotor.cs` (FR-002) (Green)
- [ ] T018 [US1] [TEST] PlayMode smoke: character in `Sandbox` scene moves and jumps from injected input in `Game/Assets/Tests/PlayMode/CharacterSmokeTests.cs` (Red)
- [ ] T019 [US1] Create scene `Game/Assets/Scenes/Sandbox.unity` (XR rig, flat floor at desk height), character view `Game/Assets/Scripts/Character/CharacterView.cs`, controller input binding `Game/Assets/Scripts/Character/CharacterInput.cs` (FR-018); sword-holding child model (FR-022) (Green)
- [ ] T020 [US1] [TEST] PlayMode: head-pose changes do not move the character; camera rig keeps real scale, in `Game/Assets/Tests/PlayMode/PeekRigTests.cs` (Red)
- [ ] T021 [US1] Implement head-tracked peeking (player stays real-scale) in `Game/Assets/Scripts/Presentation/PeekRig.cs` (FR-003) (Green)
- [ ] T022 [US1] [TEST] Posture offset: seated/standing height offsets are applied and survive recenter, in `Core/Tests/PostureTests.cs` (Red)
- [ ] T023 [US1] Implement `PostureSetup` (Core logic + Unity binding) in `Core/Model/Posture.cs` and `Game/Assets/Scripts/Presentation/PostureSetup.cs` (FR-021) (Green)

## Phase 4: User Story 2 — 机上ステージを生成して遊ぶ (P2)

**Independent Test**: 疑似机スキャンからステージが生成され、ゴールまで到達できる。同条件で同一。

- [ ] T024 [P] [US2] [TEST] `SpatialAbstractor`: desk fixture → expected cell grid (Ground/Void counts, cell size quantization), identical output across 100 runs in `Core/Tests/SpatialAbstractorTests.cs` (Red)
- [ ] T025 [US2] Implement `SpatialAbstractor` (ScanResult → integer cell grid, regions, links) in `Core/SpatialAbstraction/SpatialAbstractor.cs` (FR-004, FR-005) (Green)
- [ ] T026 [P] [US2] [TEST] Tabletop play area and hazard zones (outside table edge) computed from Table surface in `Core/Tests/TabletopPlaySpaceTests.cs` (Red)
- [ ] T027 [US2] Implement `TabletopPlaySpace` (play area, hazards) in `Core/SpatialAbstraction/TabletopPlaySpace.cs` (FR-014) (Green)
- [ ] T028 [P] [US2] [TEST] Generator determinism: same spatial data + seed → identical Stage (100 runs) in `Core/Tests/DeterminismTests.cs` (Red)
- [ ] T029 [P] [US2] [TEST] Generator reachability: start→goal reachable over 200 seeds; start/goal on distinct ground cells in `Core/Tests/ReachabilityTests.cs` (Red)
- [ ] T030 [US2] Implement exploration-stage generator (regions, start/goal at farthest reachable cells, BFS validation, deterministic retry) in `Core/StageGeneration/ExplorationGenerator.cs` (FR-007, FR-009, FR-010, FR-023) (Green)
- [ ] T031 [US2] [TEST] `StageValidator`: rejects unreachable goal, rejects area below minimum in `Core/Tests/StageValidatorTests.cs` (Red)
- [ ] T032 [US2] Implement `StageValidator` in `Core/StageGeneration/StageValidator.cs` (Green)
- [ ] T033 [P] [US2] [TEST] Scan failure (too small/empty/no table) yields a typed error, not an exception, in `Core/Tests/ScanFailureTests.cs` (Red)
- [ ] T034 [US2] Implement scan-failure results and UI message in `Core/SpatialAbstraction/ScanErrors.cs` and `Game/Assets/Scripts/Scan/ScanErrorPresenter.cs` (Green)
- [ ] T035 [US2] [TEST] PlayMode: goal trigger raises Cleared exactly once in `Game/Assets/Tests/PlayMode/StageFlowTests.cs` (Red)
- [ ] T036 [US2] Render generated stage as miniature (`StageRenderer.cs`), goal trigger + clear UI (`StageFlow.cs`), scene `Game/Assets/Scenes/Tabletop.unity` wiring FixtureScanProvider → Abstractor → Generator → Renderer (FR-013) (Green)
- [ ] T037 [US2] [TEST] Edge cases: character falling outside the stage is returned to last safe cell; tracking loss pauses the game and resumes, in `Game/Assets/Tests/PlayMode/EdgeCaseTests.cs` (Red)
- [ ] T038 [US2] Implement fall-return and tracking-loss pause in `Game/Assets/Scripts/Game/SafetyNet.cs` (Green)

## Phase 5: User Story 3 — 部屋全体をスキャンして生成 (P3)

**Independent Test**: 疑似部屋データから、家具が別の意味を持つステージが生成される。

- [ ] T039 [P] [US3] [TEST] Semantic mapping: table→Platform, shelf→Wall, sofa→Obstacle, step→cliff (Void edge) in `Core/Tests/SemanticMapperTests.cs` (Red)
- [ ] T040 [US3] Implement `SemanticMapper` in `Core/SpatialAbstraction/SemanticMapper.cs` (FR-011) (Green)
- [ ] T041 [US3] [TEST] Room fixture: all walkable regions connected or bridged; multi-level `links` are produced in `Core/Tests/RoomAbstractionTests.cs` (Red)
- [ ] T042 [US3] Implement `RoomAbstractor` (Floor/Wall/Ceiling/Furniture/Step → cell kinds, links) in `Core/SpatialAbstraction/RoomAbstractor.cs` (Green)
- [ ] T043 [US3] [TEST] Room-mode generation: determinism and reachability over 200 seeds on the room fixture in `Core/Tests/RoomGenerationTests.cs` (Red)
- [ ] T044 [US3] Extend `ExplorationGenerator` for multi-level Room mode in `Core/StageGeneration/ExplorationGenerator.cs` (Green)
- [ ] T045 [US3] [TEST] Mode selection: Tabletop/Room choice loads the matching pipeline in `Game/Assets/Tests/PlayMode/ModeSelectTests.cs` (Red)
- [ ] T046 [US3] Scene `Game/Assets/Scenes/Room.unity` and `Game/Assets/Scripts/Game/ModeSelect.cs` (FR-006) (Green)

## Phase 6: User Story 4 — 敵・アイテム・ギミックで遊ぶ (P4)

**Independent Test**: 敵とギミックのあるステージで攻撃・回避・操作しクリアできる。

- [ ] T047 [P] [US4] [TEST] Placement never blocks the start→goal path; determinism; counts per difficulty in `Core/Tests/PlacementTests.cs` (Red)
- [ ] T048 [US4] Implement rule-based `ElementPlacer` (enemies, items, chests, gimmicks, checkpoints) in `Core/StageGeneration/ElementPlacer.cs` (FR-007) (Green)
- [ ] T049 [P] [US4] [TEST] Health system: damage, Down state; CheckpointRespawn respawns at last checkpoint; NoDeath ignores enemy damage, returns only on fall, in `Core/Tests/HealthSystemTests.cs` (Red)
- [ ] T050 [US4] Implement `HealthSystem` (pure C#) in `Core/Character/HealthSystem.cs` (FR-016) (Green)
- [ ] T051 [P] [US4] [TEST] Enemy state machine (rule-based, no ML): idle→chase→attack transitions in `Core/Tests/EnemyStateTests.cs` (Red)
- [ ] T052 [US4] Implement enemy state machine in `Core/Character/EnemyBrain.cs` and Unity binding `Game/Assets/Scripts/Game/EnemyController.cs` (FR-008) (Green)
- [ ] T053 [P] [US4] [TEST] PlayMode: sword attack hits enemy in range once per swing; dodge grants brief invulnerability in `Game/Assets/Tests/PlayMode/CombatTests.cs` (Red)
- [ ] T054 [US4] Implement `CombatController` (sword attack, FR-022) in `Game/Assets/Scripts/Character/CombatController.cs` (Green)
- [ ] T055 [US4] [TEST] PlayMode: difficulty select before stage start applies the chosen mode; next-stage menu appears after clear in `Game/Assets/Tests/PlayMode/MenuFlowTests.cs` (Red)
- [ ] T056 [US4] Implement `DifficultySelect.cs` (FR-026) and `NextStageMenu.cs` (FR-024) in `Game/Assets/Scripts/Game/` (Green)

## Phase 7: User Story 5 — 複数のステージ種類 (P5)

**Independent Test**: 同一空間から各ステージ種類が生成され、いずれもクリア可能。

- [ ] T057 [P] [US5] [TEST] Each stage type (Treasure, Combat, Defense, Boss) from the same spatial data is valid, reachable and has its objective in `Core/Tests/StageTypeTests.cs` (Red)
- [ ] T058 [US5] Implement Treasure/Combat/Defense/Boss placement rules in `Core/StageGeneration/StageTypes/*.cs` (FR-012) (Green)
- [ ] T059 [P] [US5] [TEST] PlayMode: NextStageMenu lists all stage types and passes the chosen type to the generator request in `Game/Assets/Tests/PlayMode/StageTypeSelectTests.cs` (Red)
- [ ] T059a [US5] Stage type selection in `NextStageMenu.cs` (FR-024) (Green)

## Phase 8: Save, Share, Safety

- [ ] T060 [P] [TEST] SaveStore: save → reload restores spatial data, progress, settings without rescan; discard removes scan and forces rescan, in `Game/Assets/Tests/PlayMode/SaveStoreTests.cs` (Red)
- [ ] T061 Implement `SaveStore.cs` (FR-019) and `RescanFlow.cs` (FR-028) in `Game/Assets/Scripts/Save/` (Green)
- [ ] T062 [P] [TEST] ShareFile round-trip reproduces an identical Stage; export requires consent flag and omits data not needed to reproduce, in `Core/Tests/ShareRoundTripTests.cs` (Red)
- [ ] T063 Implement `ShareService.cs` with consent dialog (FR-025) in `Game/Assets/Scripts/Save/` (Green)
- [ ] T064 [P] [TEST] SafetyMonitor decision logic: passthrough on when head/controller within 30cm of boundary or hazard; off with hysteresis; manual toggle overrides, in `Core/Tests/SafetyMonitorTests.cs` (Red)
- [ ] T065 Implement `SafetyMonitor` (Core logic) and Unity binding `Game/Assets/Scripts/Presentation/SafetyMonitor.cs` (FR-014, FR-027) (Green)
- [ ] T066 [P] [TEST] `ScanResult`/`SpatialData` are immutable: attempts to mutate after creation fail to compile or throw; generator/renderer APIs take read-only inputs, in `Core/Tests/ImmutabilityTests.cs` (FR-015) (Red)
- [ ] T066a Make `ScanResult`/`SpatialData` immutable (readonly structs/records, `IReadOnlyList`) in `Core/Model/` (Green)

## Phase 9: Real scan (later, needs headset)

(T067 のスパイクは Phase 2 に移動済み)

- [ ] T068 [TEST] `MetaSceneScanProvider` conversion: recorded MRUK sample → `ScanResult` matches expected in `Game/Assets/Tests/PlayMode/MetaSceneConversionTests.cs` (Red)
- [ ] T069 Implement `MetaSceneScanProvider` in `Game/Assets/Scripts/Scan/MetaSceneScanProvider.cs` (fallback: `ManualTableScanProvider`) (Green)

## Phase 10: Polish & validation

- [ ] T070 [P] Performance budget check at end of US2 and at the end: hold 90fps in Tabletop/Room scenes (FR-029)
- [ ] T071 [P] Measure scan→generate ≤ 60s (SC-005: desk width ≥120cm, room floor area ≥10㎡) with fixture and real scan
- [ ] T072 Playtest protocol and results (SC-001 ≤3min to play, SC-004 90% unaided, SC-008 80% "immersion" survey, SC-006 zero contacts) in `specs/001-vr-miniature-world/playtest.md`
- [ ] T073 Run quickstart.md end-to-end and update it

## Dependencies

- Phase 1 → 2 → US1; US2 depends on Phase 2 (and US1 for playing); US3 depends on US2; US4 depends on US2; US5 depends on US4; Phase 8 depends on US2 (T064/T065 also on T027); Phase 9 last.
- Within a phase, `[TEST]` tasks come first; `[P]` tasks touch different files.

## Parallel examples

- Phase 2: T006 with T008
- US2: T024, T026, T028, T029 tests together, then implementations in order

## Strategy

MVP = Phase 1–3 (US1)、次に US2 でスキャン→生成→クリアの縦串を通す。実スキャンは Phase 9 で差し替え。全タスクはテスト先行(憲章 I)。
