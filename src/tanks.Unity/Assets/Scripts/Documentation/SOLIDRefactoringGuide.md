# SOLID原則を意識したタンクゲーム設計リファクタリングガイド

## 概要
本ドキュメントでは、Unity タンクゲームプロジェクトをSOLID原則に基づいてリファクタリングした内容について説明します。

## SOLID原則とは
- **S**ingle Responsibility Principle (単一責任の原則)
- **O**pen/Closed Principle (開放閉鎖の原則)
- **L**iskov Substitution Principle (リスコフの置換原則)
- **I**nterface Segregation Principle (インターフェース分離の原則)
- **D**ependency Inversion Principle (依存関係逆転の原則)

## 改善された設計

### 1. 単一責任の原則 (SRP) の適用

#### 問題点
- `GameManager` クラスが複数の責任を持っていた（ゲームループ、ラウンド管理、タンク管理、UI管理）
- `TankMovement` クラスが入力処理と物理演算を両方担当していた
- `TankShooting` クラスが入力処理、UI更新、発射処理を担当していた

#### 解決策
- **状態管理の分離**: `IGameState` インターフェースと具体的な状態クラス（`RoundStartingState` など）
- **入力処理の分離**: `IInputHandler` インターフェースと `PlayerInputHandler` クラス
- **タンク制御の分離**: `TankMovementController` は物理演算のみを担当

### 2. 開放閉鎖の原則 (OCP) の適用

#### 改善点
- `IGameState` インターフェースにより、新しいゲーム状態を既存コードを変更せずに追加可能
- `GameStateManager` により状態遷移の管理が柔軟に

```csharp
// 新しい状態を簡単に追加
public class RoundPauseState : IGameState
{
    public string StateName => "Round Paused";
    public IEnumerator Enter() { /* 実装 */ }
    public void Exit() { /* 実装 */ }
}
```

### 3. 依存関係逆転の原則 (DIP) の適用

#### 改善点
- `ITankController` インターフェースにより、具体的な実装から分離
- `IInputHandler` により入力システムを抽象化
- 依存関係の注入（Dependency Injection）パターンの採用

```csharp
// 抽象に依存し、具体に依存しない
public void SetInputHandler(IInputHandler inputHandler)
{
    _inputHandler = inputHandler;
}
```

## 新しいファイル構造

```
Scripts/
├── Interfaces/
│   ├── IGameState.cs           # ゲーム状態インターフェース
│   ├── ITankController.cs      # タンク制御インターフェース
│   └── IInputHandler.cs        # 入力処理インターフェース
├── Input/
│   └── PlayerInputHandler.cs   # プレイヤー入力実装
├── GameStates/
│   ├── GameStateManager.cs     # 状態管理マネージャー
│   └── RoundStartingState.cs   # ラウンド開始状態
└── Tank/Refactored/
    ├── TankMovementController.cs      # 移動制御（リファクタリング版）
    └── RefactoredTankManager.cs       # タンク管理（リファクタリング版）
```

## 利点

### 保守性の向上
- 各クラスが明確な責任を持つため、バグの特定と修正が容易
- コードの理解しやすさが向上

### 拡張性の向上
- 新しい機能（状態、入力システム、タンクタイプ）を既存コードを変更せずに追加可能
- プラグインアーキテクチャへの移行が容易

### テスタビリティの向上
- インターフェースによりモックオブジェクトの作成が容易
- 単体テストの書きやすさが向上

### 再利用性の向上
- コンポーネントが疎結合になり、他のプロジェクトでの再利用が可能

## 段階的移行のアプローチ

1. **インターフェースの定義**: まず抽象化レイヤーを作成
2. **新しい実装の作成**: 既存コードを変更せずに新しい実装を作成
3. **段階的な置き換え**: 一部分ずつ新しい実装に切り替え
4. **レガシーコードの削除**: 完全移行後に古いコードを削除

## 今後の改善案

1. **イベントシステムの導入**: Observer パターンによる疎結合なコミュニケーション
2. **設定の外部化**: ScriptableObject を使用した設定の管理
3. **サービスロケーターパターン**: 依存関係の管理をより洗練
4. **Command パターン**: 入力とアクションの分離

この設計により、コードベースはより保守しやすく、拡張しやすく、テストしやすくなります。 