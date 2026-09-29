# Quickstart(検証手順)

## 前提
- .NET SDK 9、Unity 6000.2.11f1(Hub で導入済み)
- 実機なし: スキャンは `Core/Fixtures/ScanFixtures.cs` の固定データを使用
- 実機あり(後日、T067〜T069): Meta Quest 3/3S を PC接続

## 1. コアの検証(Unity不要)
```
cd Core/Tests && dotnet test
```
期待: 79 件すべて成功(決定論・到達可能性・部屋抽象化・ステージ種類・共有再現など)

## 2. Unity のテスト(バッチ)
```
Unity.exe -batchmode -nographics -projectPath Game -runTests -testPlatform EditMode -testResults r.xml
Unity.exe -batchmode -nographics -projectPath Game -runTests -testPlatform PlayMode -testResults r.xml
```
期待: EditMode 79 件(Core のテスト)、PlayMode 26 件が成功

## 3. 手で遊ぶ(ヘッドセット不要)
1. Unity で `Game/` を開き、`Assets/Scenes/Tabletop.unity`(または `Room` / `Sandbox`)を開いて再生
2. 難易度を選ぶ(スティック左右/矢印キーで選択、A/Enter で決定)
3. WASD で移動、Space でジャンプ、J で攻撃、K で回避。ゴールに着くとクリアメニューが出る

## 4. 保存・共有
1. クリア後にプレイを終了して再度起動 → 再スキャンなしで同じ空間から再開(`persistentDataPath` に保存)
2. 共有は `ShareService`(同意ダイアログ → JSON を書き出し)。読み込むと同一ステージが再現される

## 未確認(実機が必要)
- 実スキャン(T067〜T069)、90fps 計測(T070)、試遊評価(T072)
