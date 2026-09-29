# Implementation Plan: VR現実空間連動型・小世界アクションゲーム

**Branch**: `001-vr-miniature-world` | **Date**: 2026-09-29 | **Spec**: [spec.md](spec.md)

## Summary

現実の机・部屋をスキャンし、決定論的なルールベース生成で小さなステージを作り、プレイヤーが巨大な存在として覗き込みながら剣士の子を操作するPCVRゲーム。
方針: **生成ロジックをUnity非依存の純C#ライブラリに分離**(決定論・テスト容易性・AI不使用の担保)し、スキャン取得とVR表示だけをUnity側の薄い層にする。

## Technical Context

**Language/Version**: C# (Unity 6 LTS 系)。生成コアは .NET Standard 2.1 互換の純C#

**Primary Dependencies**: Unity 6 / OpenXR / XR Interaction Toolkit / Meta XR SDK (Core + MR Utility Kit) — スキャン用。生成コアは外部依存なし

**Storage**: ローカルJSONファイル(空間データ・進行・設定)。共有用エクスポートも同JSON(contracts/ 参照)

**Testing**: Unity Test Framework (EditMode: 生成コア、PlayMode: 統合)。プロパティ的テスト(シード100回同一、到達可能性)を必須とする

**Target Platform**: Windows PCVR(Meta Quest 3/3S を PC接続 [Link/Air Link] で使用し、パススルー・シーンスキャンを利用)

**Project Type**: VRゲーム(Unityプロジェクト + 純C#ライブラリ)

**Performance Goals**: 90fps(FR-029)、スキャン→生成完了 ≤ 60秒(SC-005)

**Constraints**: ゲーム内AI不使用、決定論的生成、実家具は変更しない、安全境界でパススルー自動表示

**Scale/Scope**: 1人用。机上モード → 部屋モード。ステージ種類は探索型を最初に実装

## Constitution Check

`.specify/memory/constitution.md` は未策定(テンプレートのまま)のため、ゲートなし。ただし本計画の自主原則として次を置く:
1. 生成コアは UnityEngine を参照しない
2. 乱数は自前の決定論的PRNG(System.Random 非依存)のみ
3. 生成後に必ず到達可能性検証を実行する
→ 違反なし。設計後も再確認済み。

## Project Structure

### Documentation (this feature)

```text
specs/001-vr-miniature-world/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── interfaces.md
│   └── file-formats.md
└── tasks.md            # /speckit-tasks で作成
```

### Source Code (repository root)

```text
Game/                                 # Unity プロジェクト
├── Assets/
│   ├── Scripts/
│   │   ├── Scan/                     # ScanProvider 実装(Meta MRUK / 固定テスト用)
│   │   ├── Presentation/             # 小世界の描画、縮尺、覗き込み、パススルー切替
│   │   ├── Character/                # 剣士の子、入力、戦闘
│   │   ├── Game/                     # ステージ進行、難易度モード、UI
│   │   └── Save/                     # 保存・共有のIO
│   └── Tests/PlayMode/
Core/                                 # 純C#ライブラリ(Unity非依存)
├── SpatialAbstraction/               # スキャン結果 → 空間データ
├── StageGeneration/                  # ルールベース生成、検証
├── Rng/                              # 決定論的PRNG
└── Tests/                            # EditMode相当のテスト(Unityからも参照)
```

**Structure Decision**: `Core/`(純C#)+ `Game/`(Unity)の2部構成。CoreはUnityのasmdefとして取り込むか、DLLとして参照する。

## Complexity Tracking

違反なし。
