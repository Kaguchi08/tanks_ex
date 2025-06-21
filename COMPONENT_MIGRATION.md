# コンポーネント変更詳細手順

## HealthHUDコンポーネントからHealthHUDViewへの移行

### 1. 既存設定の確認

移行前に既存のHealthHUDコンポーネントの設定を確認:

```
Health Slider: HUDCanvas/HealthSlider
Fill Image: HUDCanvas/HealthSlider/Background/Fill Area/Fill
Full Health Color: 通常は緑色
Zero Health Color: 通常は赤色
Critical Health Color: 通常は黄色
Critical Health Threshold: 通常は0.25 (25%)
```

### 2. HealthHUDViewの設定詳細

#### 2.1 基本UI参照
```
[Header("UI Components")]
Health Slider: 既存のSliderコンポーネントを設定
Fill Image: Slider内のImageコンポーネントを設定
```

#### 2.2 ビジュアル設定
```
[Header("Visual Settings")]
Full Health Color: Color.green (0, 1, 0, 1)
Critical Health Color: Color.yellow (1, 1, 0, 1)  
Zero Health Color: Color.red (1, 0, 0, 1)
```

#### 2.3 アニメーション設定
```
[Header("Animation Settings")]
Animation Duration: 0.2 (200ms)
Enable Critical Animation: true (点滅効果有効)
```

### 3. 設定転送手順

#### Step 1: 既存コンポーネントの一時無効化
```
1. HealthHUD GameObjectを選択
2. HealthHUDコンポーネントのチェックボックスをOFFにする
   ※削除しない（ロールバック用に保持）
```

#### Step 2: HealthHUDViewの追加
```
1. Add Component → Scripts → Complete.UI.MVP → HealthHUDView
2. 以下のフィールドを設定:

Health Slider:
  - None (Slider) → 既存のSliderコンポーネントをドラッグ
  
Fill Image: 
  - None (Image) → Slider/Background/Fill Area/Fill のImageをドラッグ
  
Full Health Color:
  - 既存HealthHUDと同じ値を設定（通常は緑）
  
Critical Health Color:
  - Yellow (1, 1, 0, 1) を設定
  
Zero Health Color:
  - 既存HealthHUDと同じ値を設定（通常は赤）
  
Animation Duration:
  - 0.2 を設定
  
Enable Critical Animation:
  - チェックON
```

### 4. 参照確認

設定後、以下を確認:

```
Health Slider フィールド:
  ✓ "Slider (Slider)" のような表示になる
  ✗ "None (Slider)" の場合は未設定
  
Fill Image フィールド:
  ✓ "Fill (Image)" のような表示になる  
  ✗ "None (Image)" の場合は未設定
```

### 5. よくある設定ミス

#### 5.1 Fill Imageが見つからない場合
```
問題: Sliderの構造が異なる場合
解決方法:
1. Sliderを展開して子オブジェクトを確認
2. 一般的な構造:
   Slider
   ├── Background (Image)
   └── Fill Area
       └── Fill (Image) ← これを設定
```

#### 5.2 色が正しく反映されない場合
```
問題: Fill ImageのColor ModeがVertex Colorの場合
解決方法:
1. Fill ImageのInspectorを開く
2. Color → Simple に変更
3. Colorフィールドが表示されることを確認
```

### 6. MVPHUDManagerとの接続

HealthHUDViewの設定が完了したら:

```
1. MVPHUDManagerのInspectorを開く
2. Health HUD View フィールドに設定したHealthHUDViewをドラッグ
3. "HealthHUDView (HealthHUDView)" のような表示になることを確認
```

### 7. 設定確認チェックリスト

移行前チェック:
- [ ] 既存HealthHUDの設定値を記録
- [ ] SliderとImageの参照パスを確認
- [ ] シーンのバックアップを作成

移行中チェック:
- [ ] HealthHUDコンポーネントを無効化（削除しない）
- [ ] HealthHUDViewコンポーネントを追加
- [ ] 全フィールドを正しく設定
- [ ] MVPHUDManagerに接続

移行後チェック:
- [ ] コンパイルエラーなし
- [ ] Play Mode でエラーなし  
- [ ] 初期化ログの確認
- [ ] HP表示の動作確認

### 8. トラブルシューティング

#### 問題: "HealthHUDView script is missing"
```
原因: スクリプトファイルのコンパイルエラー
解決: Console でエラーを確認し、MVPファイルのコンパイルを修正
```

#### 問題: SliderやImageの参照が設定できない
```
原因: GameObjectの構造が想定と異なる
解決: Hierarchy でUI構造を確認し、正しいコンポーネントを特定
```

#### 問題: 色の変更が反映されない
```
原因: Image コンポーネントの設定問題
解決: 
1. Image.raycastTarget = false に設定
2. Image.Type = Filled に設定
3. Image.FillMethod = Horizontal に設定
```