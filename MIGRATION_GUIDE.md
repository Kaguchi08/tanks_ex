# HUDシステム MVP移行ガイド

## 移行概要

既存のHUDシステムから新しいMVPパターンベースのシステムへの段階的移行手順です。

## Phase 1: 準備段階

### 1.1 バックアップの作成
```bash
# 現在のシーンとスクリプトをバックアップ
cp Assets/_Complete-Game.unity Assets/_Complete-Game_backup.unity
```

### 1.2 コンパイルエラーの確認
```bash
# Unity Editorでコンパイルエラーがないことを確認
# MVPシステムの全ファイルが正しくコンパイルされることを確認
```

## Phase 2: Unity Editor設定

### 2.1 MVPHUDManagerの設定

1. **GameObjectの作成**
   ```
   Hierarchy → 右クリック → Create Empty GameObject
   名前: "MVPHUDManager"
   ```

2. **コンポーネントの追加**
   ```
   MVPHUDManagerコンポーネントをアタッチ
   ```

3. **フィールドの設定**
   ```
   HUD Canvas: 既存のHUDCanvasを設定
   Health HUD View: 後で設定（次のステップで作成）
   ```

### 2.2 HealthHUDViewの設定

1. **既存HealthHUDの処理**
   ```
   HUDCanvas/HealthHUD GameObjectを選択
   既存のHealthHUDコンポーネントを無効化（チェックOFF）
   ※削除はしない（ロールバック用）
   ```

2. **HealthHUDViewの追加**
   ```
   HealthHUDViewコンポーネントを追加
   Health Slider: 既存のSliderを設定
   Fill Image: 既存のSlider/Fill/Imageを設定
   Full Health Color: Green
   Critical Health Color: Yellow  
   Zero Health Color: Red
   Animation Duration: 0.2
   Enable Critical Animation: チェックON
   ```

3. **MVPHUDManagerとの接続**
   ```
   MVPHUDManagerのHealth HUD Viewフィールドに
   作成したHealthHUDViewを設定
   ```

### 2.3 GameManagerの設定

1. **フィールドの設定**
   ```
   MVP HUD Manager: 作成したMVPHUDManagerを設定
   HUD Manager: 既存を保持（フォールバック用）
   ```

## Phase 3: 動作確認

### 3.1 初期確認
```
1. Play Modeに入る
2. Console で以下のログを確認:
   - "MVPHUDManager Initialize Start"
   - "Health HUD MVP created"
   - "MVPHUDManager initialized"
   - "HealthHUDPresenter started"
```

### 3.2 HP同期確認
```
1. タンクを生成
2. Console で以下のログを確認:
   - "SetupPlayerHUD (MVP): Tank 0"
   - "MVPHUDManager: Player health provider set"
   - "HealthHUDPresenter: Health updated"
```

### 3.3 ダメージ確認
```
1. タンクが被ダメージを受ける
2. HUD SliderとColorが正しく更新されることを確認
3. クリティカル状態（25%以下）で点滅アニメーションを確認
```

## Phase 4: トラブルシューティング

### 4.1 よくある問題

**問題**: MVPHUDManagerが初期化されない
```
確認事項:
- MVPHUDManagerコンポーネントが正しくアタッチされているか
- HUD CanvasとHealth HUD Viewが正しく設定されているか
- Console でエラーログがないか確認
```

**問題**: HP同期しない
```
確認事項:
- TankHealthがIHealthProviderを実装しているか確認
- GameManagerでMVPHUDManagerが優先されているか確認
- HealthHUDPresenterがModelを受け取っているか確認
```

**問題**: UIが表示されない
```
確認事項:
- HUD Canvasがアクティブか確認
- HealthHUDViewコンポーネントが有効か確認
- SliderとImageの参照が正しく設定されているか確認
```

### 4.2 ロールバック手順

問題が発生した場合のロールバック:

```
1. MVPHUDManagerコンポーネントを無効化
2. 既存HealthHUDコンポーネントを有効化
3. GameManagerのMVP HUD Managerフィールドをクリア
4. 既存HUD Managerが動作することを確認
```

## Phase 5: レガシーシステムの段階的削除

### 5.1 完全移行後の処理（推奨：1週間の安定稼働後）

```
1. 既存HealthHUDコンポーネントを削除
2. 古いHUDManagerの参照をGameManagerから削除
3. 不要になったレガシーファイルを削除:
   - HUDManager.cs (旧)
   - HealthHUD.cs (旧)
   - HUDBase.cs (必要に応じて)
```

### 5.2 コードクリーンアップ

```
1. GameManagerから以下のメソッドを削除:
   - SetupPlayerHUDWithLegacy()
   - レガシー用のフォールバック処理

2. 不要なusing文を削除
```

## 移行チェックリスト

### 事前確認
- [ ] 全MVPファイルのコンパイル成功
- [ ] 既存システムの正常動作確認
- [ ] バックアップファイルの作成

### Unity Editor設定
- [ ] MVPHUDManagerの作成・設定
- [ ] HealthHUDViewの追加・設定
- [ ] GameManagerのフィールド設定

### 動作確認
- [ ] 初期化ログの確認
- [ ] HP同期の確認
- [ ] ダメージ処理の確認
- [ ] クリティカル状態の確認

### 最終確認
- [ ] 1週間の安定稼働
- [ ] レガシーコードの削除
- [ ] コードクリーンアップ

## 注意事項

1. **段階的移行**: 一度に全てを変更せず、段階的に移行する
2. **ロールバック準備**: 問題が発生した場合に即座に戻せるよう準備
3. **十分なテスト**: 各段階で動作確認を行う
4. **ドキュメント化**: 変更内容を記録して共有する

## 今後の拡張

MVPシステムが安定稼働したら、以下の新機能を同様のパターンで実装:

1. **AmmoHUD**: 弾薬表示
2. **ScoreHUD**: スコア表示
3. **MiniMapHUD**: ミニマップ
4. **StatusHUD**: ゲーム状態表示

各機能は以下の構成で実装:
- Model: データとビジネスロジック
- View: UI表示とアニメーション
- Presenter: ModelとViewの仲介
- Factory: MVP構成要素の生成