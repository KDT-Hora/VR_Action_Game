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

## Core の補助型(責務)
- `ScaleSettings`: キャラクターサイズ。5〜20cmに丸め、既定10cm
- `Posture`: 座位/立位の高さオフセット。再センタリング後も保持
- `CharacterMotor`: 入力→位置・速度、接地時のみジャンプ、重力
- `HealthSystem`: HP、Down状態、難易度モードごとの復活/落下復帰
- `EnemyBrain`: 敵のルールベース状態機械(idle→chase→attack)。機械学習は使わない
- `SafetyMonitor`: 頭部・コントローラーが境界/危険区域の30cm以内でパススルーON、ヒステリシス付き、手動切替が優先
- `ScanResult` / `SpatialData` は生成後に変更不可(イミュータブル)
