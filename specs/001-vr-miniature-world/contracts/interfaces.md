# Internal Interface Contracts

## Core(純C#)
- `ISpatialAbstractor.Abstract(ScanResult, AbstractionOptions) : SpatialData`
  - 決定論的(同一入力→同一出力)
- `IStageGenerator.Generate(SpatialData, GenerationRequest) : GenerationOutcome`
  - `GenerationOutcome = Success(Stage) | Failure(reason)`
  - 成功時は `start→goal` の到達可能性が検証済み
  - 副作用なし、時間・環境・グローバル乱数に依存しない
- `IStageValidator.Validate(Stage, SpatialData) : ValidationReport`
- `IDeterministicRng`: `Next()`, `Range(min,max)`, `Fork(label)`

## Unity層
- `IScanProvider.ScanAsync(ScanMode) : ScanResult`
  - 実装: `MetaSceneScanProvider`, `FixtureScanProvider`(テスト・開発用), `ManualTableScanProvider`(代替)
- `ISaveStore.Load/Save(SaveFile)`, `IShareService.Export/Import(ShareFile)`
- `ISafetyMonitor`: 境界接近イベント→パススルー切替
