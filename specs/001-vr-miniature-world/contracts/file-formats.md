# File Formats(JSON, versioned)

## SaveFile v1
```json
{ "version": 1, "spatialData": {...}, "clearedStages": [{"seed": 0, "stageType": "Exploration"}],
  "settings": {"characterScaleCm": 10, "difficulty": "CheckpointRespawn"} }
```

## ShareFile v1
```json
{ "version": 1, "seed": 123456789, "stageType": "Exploration", "difficulty": "NoDeath",
  "characterScaleCm": 10, "spatialData": {...}, "spatialDataHash": "sha256:..." }
```
- インポート時に `spatialDataHash` を検証し、不一致は拒否
- 未知の `version` は読み込み拒否(エラーメッセージ表示)
