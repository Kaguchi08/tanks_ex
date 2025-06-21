# MVP HUDシステム動作確認ガイド

## テスト環境準備

### 1. 事前チェック
```
□ Unity Editor でコンパイルエラーなし
□ MVPHUDManager が正しく設定済み
□ HealthHUDView が正しく設定済み
□ GameManager で MVP HUD Manager が設定済み
□ Console に初期エラーがない
```

### 2. テストシナリオ

## Phase 1: 初期化テスト

### Test 1.1: MVPHUDManager初期化
```
手順:
1. Play Mode に入る
2. Console ログを確認

期待結果:
✓ "=== MVPHUDManager Initialize Start ==="
✓ "MVPHUDManager: Canvas auto-detected - HUDCanvas"
✓ "MVPHUDManager: HealthHUDView auto-detected - HealthHUD"
✓ "HUDFactory: Health HUD MVP created for HealthHUD"
✓ "MVPHUDManager initialized with 1 presenters"
✓ "=== MVPHUDManager Initialize End ==="

失敗パターン:
✗ "MVPHUDManager initialization failed"
✗ "HealthHUDView not found"
✗ "Failed to create Health HUD presenter"
```

### Test 1.2: タンク生成とHUD接続
```
手順:
1. ゲーム開始を待つ
2. タンクが生成されるのを確認
3. Console ログを確認

期待結果:
✓ "=== SetupPlayerHUD (MVP): Tank 0 ==="
✓ "TankHealth found: TankHealth"
✓ "Current Health: 100"
✓ "Max Health: 100"
✓ "MVPHUDManager: Player health provider set successfully"
✓ "HealthHUDPresenter: Health updated - 100/100 (100%)"

失敗パターン:
✗ "=== SetupPlayerHUD (Legacy): Tank 0 ===" (レガシー版が実行)
✗ "TankHealth component not found"
✗ "Health HUD presenter not found"
```

## Phase 2: HP表示テスト

### Test 2.1: 初期HP表示
```
手順:
1. タンク生成後、HUD Sliderを確認
2. HP表示の初期状態を確認

期待結果:
✓ Slider.value = 1.0 (100%)
✓ Fill Color = 緑色
✓ クリティカルアニメーションなし

確認方法:
- HUDCanvas/HealthSlider の Slider コンポーネント
- Fill Area/Fill の Image.color
```

### Test 2.2: HP減少テスト
```
手順:
1. タンクに砲弾を当ててダメージを与える
2. HUD の変化を確認
3. Console ログを確認

期待結果:
✓ Slider値がダメージに応じて減少
✓ 色が HP割合に応じて変化（緑→黄→赤）
✓ スムーズなアニメーション（0.2秒）
✓ Console: "TankHealth.TakeDamage: 100 -> 80 (damage: 20)"
✓ Console: "HealthHUDPresenter: Health updated - 80/100 (80%)"

失敗パターン:
✗ Slider値が変化しない
✗ 色が変化しない
✗ アニメーションがガクガクする
✗ 同期ログが出力されない
```

### Test 2.3: クリティカル状態テスト
```
手順:
1. HPを25%以下まで減らす
2. クリティカル状態の視覚効果を確認

期待結果:
✓ 点滅アニメーション開始（赤⇔黄色）
✓ 点滅間隔 0.3秒
✓ Console: "HealthHUDPresenter: Critical state updated - true"

確認ポイント:
- Fill Image の色が定期的に変化する
- ゲームプレイに支障がない程度の点滅速度
```

### Test 2.4: 死亡状態テスト
```
手順:
1. HPを0まで減らす
2. 死亡時の表示を確認

期待結果:
✓ Slider.value = 0
✓ Fill Color = 赤色
✓ 点滅アニメーション停止
✓ Console: "HealthHUDPresenter: Death state updated"

確認ポイント:
- 死亡後も HUD が正しく表示される
- 点滅が確実に停止する
```

## Phase 3: 異常系テスト

### Test 3.1: コンポーネント欠損テスト
```
テストケース:
1. Health Slider 参照なし
2. Fill Image 参照なし
3. MVPHUDManager なし

手順:
1. 各フィールドを意図的にクリア
2. Play Mode に入る
3. エラーハンドリングを確認

期待結果:
✓ 適切なエラーメッセージ
✓ ゲーム全体のクラッシュなし
✓ レガシーシステムへのフォールバック
```

### Test 3.2: パフォーマンステスト
```
手順:
1. Profiler を開く
2. HP変更を連続実行
3. メモリリークがないか確認

期待結果:
✓ フレームレート安定
✓ メモリ使用量が安定
✓ GC.Alloc が過度でない

確認ポイント:
- Update() での不要な処理なし
- UniRx Subscription の適切な管理
- CompositeDisposable の正常動作
```

## Phase 4: 統合テスト

### Test 4.1: ネットワークモード互換性
```
手順:
1. ネットワークモードでゲーム開始
2. 複数プレイヤーでの動作確認
3. HP同期の確認

期待結果:
✓ 自分のHPのみ HUD に表示
✓ 他プレイヤーのHP変化は HUD に影響しない
✓ ネットワーク通信の阻害なし
```

### Test 4.2: ゲームループ統合
```
手順:
1. 複数ラウンドをプレイ
2. ラウンド開始/終了時の HUD 動作確認
3. シーン再読み込み時の動作確認

期待結果:
✓ ラウンド開始時に HP 100% 表示
✓ ラウンド終了時に HUD 適切に非表示
✓ シーン再読み込み後の正常初期化
```

## トラブルシューティング

### 1. よくある問題と解決策

#### 問題: 初期化エラー
```
エラー: "MVPHUDManager initialization failed"
原因: コンポーネント参照の設定不備
解決:
1. MVPHUDManager の HUD Canvas 設定確認
2. Health HUD View 設定確認
3. HealthHUDView の Slider/Image 参照確認
```

#### 問題: HP同期しない
```
エラー: HP変更時にHUDが更新されない
原因: Model-Presenter間の接続不備
解決:
1. TankHealth が IHealthProvider を実装しているか確認
2. SetPlayerHealthProvider() が呼ばれているか確認
3. ReactiveProperty の Subscribe が動作しているか確認
```

#### 問題: UI表示異常
```
エラー: Sliderが動くが色が変わらない
原因: Image コンポーネントの設定問題
解決:
1. Fill Image の参照が正しいか確認
2. Image.Type = Filled 設定確認
3. Image.Color Mode = Simple 設定確認
```

### 2. デバッグ手法

#### ログレベル調整
```csharp
// デバッグ用: 詳細ログを有効化
// HealthHUDPresenter.cs の UpdateHealthDisplay() にログ追加
Debug.Log($"Health Update: {normalizedHealth:P0}, Color: {healthColor}");

// リリース用: 重要なログのみ残す
// Debug.Log → Debug.LogWarning/Error のみ
```

#### ブレークポイント設定
```
推奨設定箇所:
1. MVPHUDManager.SetPlayerHealthProvider()
2. HealthHUDPresenter.UpdateHealthDisplay()
3. HealthHUDView.UpdateHealthValue()
4. GameManager.SetupPlayerHUDWithMVP()
```

## 成功基準

### 必須要件
```
✓ コンパイルエラーなし
✓ 初期化エラーなし
✓ HP変更が即座にHUDに反映
✓ 色の変化が正常
✓ クリティカル状態の点滅動作
✓ 死亡時の正しい表示
✓ パフォーマンス劣化なし
```

### 推奨要件
```
✓ スムーズなアニメーション
✓ 適切なエラーハンドリング
✓ メモリリークなし
✓ ネットワークモード互換性
✓ 複数ラウンド安定動作
```

## 移行判定

以下の条件を満たした場合、移行成功と判定:

### 1週間安定稼働後
```
□ 上記テスト項目全てクリア
□ 実際のゲームプレイで問題なし
□ パフォーマンス劣化報告なし
□ ユーザーからの不具合報告なし
```

この条件をクリアしたら、レガシーシステムの段階的削除を開始できます。