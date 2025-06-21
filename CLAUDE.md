# CLAUDE.md

このファイルは、Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンスを提供します。

## プロジェクト概要

これはクライアント・サーバー アーキテクチャを持つUnityベースのマルチプレイヤー戦車対戦ゲームです。このプロジェクトは、SOLID原則、Stateパターン、コンポーネントベース設計などの現代的なC#開発手法を実装しています。

## アーキテクチャ

### 主要コンポーネント
- **サーバー (`src/tanks.Server/`)**: MagicOnion 7.0.4を使用したリアルタイムマルチプレイヤー通信のためのASP.NET Coreサーバー
- **共有ライブラリ (`src/tanks.Shared/`)**: クライアントとサーバーで共有される共通インターフェースとデータモデル
- **Unityクライアント (`src/tanks.Unity/`)**: Universal Render Pipeline (URP)を使用した3D戦車対戦ゲーム

### 主要技術
- **MagicOnion**: Unity向けのgRPCベースリアルタイム通信フレームワーク
- **UniTask**: Unity用の非同期/await サポート
- **UniRx**: Unity用のReactive Extensions
- **YetAnotherHttpHandler**: Unityネットワーキング用のHTTP/2クライアント
- **MessagePack**: ネットワーク通信用バイナリシリアル化

## 開発コマンド

### サーバー開発
```bash
# サーバーのビルドと実行
cd src/tanks.Server
dotnet build
dotnet run

# パッケージの復元
dotnet restore
```

### Unity開発
Unityプロジェクトは `src/tanks.Unity/` にあります。Unity Editor 2022.3.62f1以降を使用してください。

### ソリューション管理
```bash
# ソリューション全体のビルド
dotnet build tanks.sln

# 全パッケージの復元
dotnet restore tanks.sln
```

## コード構造

### デザインパターン
- **Stateパターン**: `IGameState`を通じて管理されるゲーム状態（`RoundStartingState`, `RoundPlayingState`, `RoundEndingState`）
- **Strategyパターン**: `IInputProvider`を介した入力プロバイダー（`LocalInputProvider`, `AIInputProvider`, `RemoteInputProvider`）
- **Facadeパターン**: 戦車システムへの統一インターフェースを提供する`TankManager`
- **Componentパターン**: `TankMovement`, `TankShooting`, `TankHealth`によるUnityのコンポーネントベースアーキテクチャ
- **MVPパターン**: UIシステムで完全なModel-View-Presenterアーキテクチャを実装
  - HealthHUD（体力表示）、GameTimer（時間表示）、RoundCount（ラウンド進行）の各HUDシステム
  - `MVPHUDManager`による統合管理とUnity Editor対応の非同期初期化システム
  - `TaskCompletionSource`を使用した確実な初期化待機メカニズム

### SOLID原則の実装
- **SRP**: 各コンポーネントは単一の責任を持つ（移動、射撃、体力）
- **OCP**: 既存のコードを修正せずにインターフェースを通じて拡張可能
- **ISP**: 特化されたインターフェース（`IGameState`, `ITankController`, `IInputProvider`）
- **DIP**: 高レベルモジュールは具象ではなく抽象に依存

### ネットワーキング
- `ITankGameHub`がマルチプレイヤー通信の契約を定義
- `TankPositionData`が同期の主要データ構造
- `Services/TankGameHub.cs`でのサーバーハブ実装  
- `MagicOnionInitializer.cs`でのクライアント接続初期化

## 理解すべき重要ファイル

### サーバー
- `src/tanks.Server/Program.cs`: サーバーの起動と設定
- `src/tanks.Server/Services/TankGameHub.cs`: マルチプレイヤーハブの実装

### 共有ライブラリ
- `src/tanks.Shared/ITankGameHub.cs`: ネットワーク通信インターフェース
- `src/tanks.Shared/IMyFirstService.cs`: サービス契約

### Unityクライアント
- `src/tanks.Unity/Assets/Scripts/Managers/GameManager.cs`: メインゲームロジックコントローラー
- `src/tanks.Unity/Assets/Scripts/Managers/TankManager.cs`: 個別戦車管理
- `src/tanks.Unity/Assets/Scripts/MagicOnionInitializer.cs`: ネットワーク初期化
- `src/tanks.Unity/Assets/Scripts/UI/MVP/Managers/MVPHUDManager.cs`: MVPパターンによるHUD統合管理
- `src/tanks.Unity/Assets/Scripts/UI/MVP/Factories/HUDFactory.cs`: MVP要素の生成とDI管理
- `src/tanks.Unity/Assets/Scripts/UI/MVP/Models/`: MVP Modelクラス（HealthHUD、GameTimer、RoundCount）
- `src/tanks.Unity/Assets/Scripts/UI/MVP/Views/`: MVP Viewクラス（UI表示制御）
- `src/tanks.Unity/Assets/Scripts/UI/MVP/Presenters/`: MVP Presenterクラス（Model-View仲介）
- `src/tanks.Unity/Assets/Scripts/Interfaces/IHealthProvider.cs`: 体力情報提供インターフェース

## テストとビルド

コードベースに特定のテストコマンドは見つかりませんでした。Unity固有のテストにはUnityの組み込みテストランナーを使用し、サーバーコンポーネントには標準の.NETテストを使用してください。

## ネットワークアーキテクチャ

ゲームはクライアント・サーバーモデルを使用しています：
- UnityクライアントはMagicOnionを通じてASP.NET Coreサーバーに接続
- リアルタイム通信はgRPCストリーミングで実現
- サーバーがゲーム状態を管理し、接続されたクライアントに更新を配信
- 共有ライブラリがクライアントとサーバー間の型安全性を保証

## カスタムプロンプト
- 常に日本語で回答して
- 実装後に全体を俯瞰して他に修正すべき項目がないか確認して
- 実装後に全体の設計を見直して適切か確認して