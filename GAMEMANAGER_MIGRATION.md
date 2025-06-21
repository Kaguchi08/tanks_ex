# GameManager移行手順

## HUDManager参照の切り替え

### 1. 現在のGameManager設定

GameManagerのInspectorで以下の設定を確認:

```
[Header("References")]
Camera Control: カメラコントローラー
Message Text: UIテキスト  
Tank Prefab: タンクプレハブ
Tanks: TankManagerの配列
Network Manager: ネットワークマネージャー
HUD Manager: 既存のHUDManager ← レガシー
MVP HUD Manager: (空) ← 新しく設定
```

### 2. MVP HUD Managerの設定

#### Step 1: MVPHUDManagerの作成確認
```
1. Hierarchy で "MVPHUDManager" GameObjectが存在することを確認
2. MVPHUDManagerコンポーネントがアタッチされていることを確認
3. Health HUD View が正しく設定されていることを確認
```

#### Step 2: GameManagerへの接続
```
1. GameManagerのInspectorを開く
2. "MVP HUD Manager" フィールドを確認
3. MVPHUDManager GameObjectをドラッグ&ドロップ
4. "MVPHUDManager (MVPHUDManager)" と表示されることを確認
```

### 3. 優先度の動作確認

GameManagerのSetupPlayerHUD()メソッドの動作:

```csharp
private void SetupPlayerHUD(int tankIndex)
{
    // MVPHUDManagerを優先して使用
    if (m_MVPHUDManager != null)
    {
        SetupPlayerHUDWithMVP(tankIndex);      // ← MVP版を実行
    }
    else if (m_HUDManager != null)
    {
        SetupPlayerHUDWithLegacy(tankIndex);   // ← レガシー版を実行
    }
    else
    {
        Debug.LogWarning("HUDManagerが設定されていません（MVP・レガシー両方とも）");
    }
}
```

### 4. 段階的移行戦略

#### 戦略A: 即座切り替え（推奨）
```
1. MVP HUD Manager を設定
2. HUD Manager は残しておく（フォールバック用）
3. MVP版が優先実行される
4. 問題があれば MVP HUD Manager をクリアして即座にロールバック
```

#### 戦略B: 並行稼働テスト
```
1. 両方のHUDManagerを設定
2. Game Managerで手動切り替えのフラグを追加
3. 十分テストしてからMVP版に統一
```

### 5. 動作確認ログ

正常な移行時に確認すべきConsoleログ:

#### 初期化段階
```
=== MVPHUDManager Initialize Start ===
MVPHUDManager: Canvas auto-detected - HUDCanvas
MVPHUDManager: HealthHUDView auto-detected - HealthHUD
HUDFactory: Health HUD MVP created for HealthHUD
MVPHUDManager: Presenter registered - IHealthHUDPresenter
MVPHUDManager initialized with 1 presenters
=== MVPHUDManager Initialize End ===
```

#### タンク生成段階
```
=== SetupPlayerHUD (MVP): Tank 0 ===
TankHealth found: TankHealth
Current Health: 100
Max Health: 100
MVPHUDManager: Player health provider set successfully
HealthHUDPresenter: Subscribed to model events
HealthHUDPresenter: Health updated - 100/100 (100%)
MVPHUDManager: プレイヤータンク（Slot:0）のHPを設定しました
```

#### HP変更段階
```
TankHealth.TakeDamage: 100 -> 80 (damage: 20)
HealthHUDPresenter: Health updated - 80/100 (80%)
```

### 6. トラブルシューティング

#### 問題: MVP版が実行されない
```
確認項目:
- MVP HUD Manager フィールドが正しく設定されているか
- MVPHUDManagerコンポーネントが有効になっているか
- Console で初期化エラーがないか確認

デバッグ方法:
- SetupPlayerHUD() にブレークポイントを設定
- m_MVPHUDManager が null でないことを確認
```

#### 問題: HP同期しない
```
確認項目:
- HealthHUDView が正しく設定されているか
- TankHealth が IHealthProvider を実装しているか
- HealthHUDPresenter が正しく初期化されているか

デバッグ方法:
- SetupPlayerHUDWithMVP() にブレークポイントを設定
- m_MVPHUDManager.SetPlayerHealthProvider() の呼び出しを確認
```

#### 問題: UI が表示されない
```
確認項目:
- HUD Canvas がアクティブか
- HealthHUDView コンポーネントが有効か
- Slider と Image の参照が正しく設定されているか

デバッグ方法:
- m_MVPHUDManager.ShowAll() が呼ばれているか確認
- HealthHUDView.Show() が呼ばれているか確認
```

### 7. ロールバック手順

問題が発生した場合の緊急ロールバック:

#### 方法1: MVP無効化
```
1. GameManagerのInspectorを開く
2. MVP HUD Manager フィールドをクリア（None に設定）
3. レガシーHUD Manager が動作することを確認
4. Console で "SetupPlayerHUD (Legacy)" ログを確認
```

#### 方法2: MVPHUDManager無効化
```
1. MVPHUDManager GameObjectを選択
2. MVPHUDManagerコンポーネントのチェックボックスをOFF
3. GameManagerが自動的にレガシー版にフォールバック
```

### 8. 移行チェックリスト

#### 事前準備
- [ ] バックアップシーンの作成
- [ ] 既存システムの正常動作確認
- [ ] MVPシステムのコンパイル確認

#### 移行実施
- [ ] MVPHUDManager GameObjectの作成
- [ ] MVPHUDManagerコンポーネントの設定
- [ ] HealthHUDViewコンポーネントの設定
- [ ] GameManagerのMVP HUD Manager設定

#### 動作確認
- [ ] 初期化ログの確認
- [ ] タンク生成時のログ確認
- [ ] HP同期の動作確認
- [ ] UI表示の確認
- [ ] クリティカル状態の確認

#### 最終確認
- [ ] 複数回のPlayModeテスト
- [ ] 異常ケースのテスト（HP 0、満タン等）
- [ ] パフォーマンス確認（フレームレート等）
- [ ] ロールバック手順の確認

### 9. 成功の判定基準

移行が成功したと判定できる条件:

```
✓ Console にエラーログが出ない
✓ "SetupPlayerHUD (MVP)" ログが出力される
✓ HP変更時にHUD Sliderが正しく更新される
✓ HP色が正しく変化する（緑→黄→赤）
✓ クリティカル状態で点滅アニメーションが動作する
✓ 死亡時にHP 0% の表示になる
✓ 既存の機能に影響がない
```

### 10. 移行後の最適化

移行が成功して安定稼働したら:

#### 1週間後
```
- レガシーHUDManagerの参照をクリア
- SetupPlayerHUDWithLegacy() メソッドを削除
- 不要なフォールバック処理を削除
```

#### 1ヶ月後
```
- 旧HealthHUDコンポーネントを削除
- 旧HUDManagerファイルを削除
- 旧HUDBaseファイルを削除（必要に応じて）
```