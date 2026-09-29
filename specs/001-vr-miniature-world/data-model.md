# Data Model

## ScanResult(入力、スキャン由来)
- `id`, `capturedAt`, `source`(Meta/Fixture/Manual)
- `surfaces[]`: `kind`(Floor/Wall/Ceiling/Table/Furniture/Other), `polygon`(3D頂点列), `heightRange`
- `bounds`: 部屋の外形

## SpatialData(抽象化後)
- `version`, `cellSize`(m)
- `grid`: セル配列 `{x, y, z, kind: Ground|Wall|Obstacle|Platform|Void, height}`
- `regions[]`: 連結領域 `{id, cells, level}`
- `links[]`: 領域間の接続(段差/隣接)
- `playArea`: プレイ可能範囲、`hazards[]`: 危険区域
- 検証: 歩行可能領域が存在すること、最小面積以上

## PlaySpace
- `mode`(Tabletop|Room), `origin`, `playerAnchor`, `boundary`

## GenerationRequest
- `seed`(uint64), `stageType`(Exploration|Treasure|Combat|Defense|Boss), `difficulty`, `characterScale`, `spatialDataHash`

## Stage
- `request`, `start`, `goal`, `path`(検証済み経路), `elements[]`, `objective`
- 検証: `start→goal` がBFSで到達可能

## StageElement
- `kind`(Enemy|Item|Gimmick|Chest|Boss|Obstacle|Checkpoint), `cell`, `params`

## Character
- `scale`, `hp`, `maxHp`, `state`(Alive|Down), `actions`(Move|Jump|Attack|Dodge|Interact)
- Difficulty: `CheckpointRespawn` | `NoDeath`(落下時のみ安全位置へ戻す)

## Progress / SaveFile
- `spatialData`, `clearedStages[]`, `settings`(scale, difficulty), `version`

## 状態遷移
- Stage: `Generated → Playing → Cleared | Retry`
- Character(CheckpointRespawn): `Alive → Down → Respawn`
