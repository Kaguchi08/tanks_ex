# レガシーシステム段階的削除ガイド

## 削除タイムライン

### 前提条件
- MVP システムが1週間以上安定稼働
- 全テストケースがパス
- ユーザーからの不具合報告なし
- パフォーマンス劣化なし

## Phase 1: 参照の削除（移行後1週間）

### Step 1.1: GameManagerの最適化

現在のコード:
```csharp
public HUDManager m_HUDManager;  // レガシー（削除対象）
public Complete.UI.MVP.MVPHUDManager m_MVPHUDManager;  // 新システム

private void SetupPlayerHUD(int tankIndex)
{
    // MVPHUDManagerを優先して使用
    if (m_MVPHUDManager != null)
    {
        SetupPlayerHUDWithMVP(tankIndex);
    }
    else if (m_HUDManager != null)  // ← フォールバック削除
    {
        SetupPlayerHUDWithLegacy(tankIndex);  // ← メソッド削除
    }
    else
    {
        Debug.LogWarning("HUDManagerが設定されていません（MVP・レガシー両方とも）");
    }
}
```

最適化後:
```csharp
public Complete.UI.MVP.MVPHUDManager m_HUDManager;  // 統一名称

private void SetupPlayerHUD(int tankIndex)
{
    if (m_HUDManager == null)
    {
        Debug.LogWarning("HUDManagerが設定されていません");
        return;
    }

    TankHealth tankHealth = m_Tanks[tankIndex].GetTankHealth();
    if (tankHealth != null)
    {
        m_HUDManager.SetPlayerHealthProvider(tankHealth);
        m_HUDManager.ShowAll();
        Debug.Log($"HUDManager: プレイヤータンク（Slot:{tankIndex}）のHPを設定しました");
    }
    else
    {
        Debug.LogError($"TankHealth component not found on tank {tankIndex}");
    }
}
```

### Step 1.2: Unity Inspector の更新

```
1. GameManagerのInspectorを開く
2. 新しい"HUD Manager"フィールドにMVPHUDManagerを設定
3. 旧フィールドは自動的に消える（コードから削除後）
```

## Phase 2: コンポーネントの削除（移行後2週間）

### Step 2.1: 旧HealthHUDコンポーネントの削除

```
1. HealthHUD GameObjectを選択
2. Inspector で無効化されている旧HealthHUDコンポーネントを確認
3. Remove Component で削除
4. HealthHUDViewコンポーネントのみ残る
```

### Step 2.2: 旧HUDManagerの削除

```
1. 旧HUDManager GameObjectを探す（通常はGameManagerの子など）
2. 参照されていないことを確認
3. GameObject を削除
```

## Phase 3: スクリプトファイルの削除（移行後1ヶ月）

### Step 3.1: 削除対象ファイルの特定

削除予定ファイル:
```
Assets/Scripts/UI/HUD/
├── HUDManager.cs (旧)           ← 削除
├── HealthHUD.cs (旧)            ← 削除  
├── HUDBase.cs                   ← 確認後削除
├── Interfaces/
│   ├── IHUDElement.cs (旧)      ← 削除
│   ├── IHUDManager.cs (旧)      ← 削除
│   ├── IHealthDisplay.cs (旧)   ← 削除
│   └── IHealthProvider.cs       ← 保持（MVP でも使用）
```

保持するファイル:
```
Assets/Scripts/UI/MVP/           ← 全て保持
Assets/Scripts/UI/HUD/Interfaces/IHealthProvider.cs  ← 保持
```

### Step 3.2: 削除前の確認

```bash
# 1. 削除対象ファイルの参照確認
grep -r "using Complete.UI.HUD;" Assets/Scripts/ --include="*.cs"
grep -r "HUDManager" Assets/Scripts/ --include="*.cs" | grep -v MVP
grep -r "HealthHUD" Assets/Scripts/ --include="*.cs" | grep -v MVP

# 2. コンパイルエラーがないことを確認
# Unity Editor でコンパイル成功を確認

# 3. 参照されていないことを確認
# Unity Editor の Project Search で各ファイルを検索
```

### Step 3.3: ファイル削除の実行

```bash
# バックアップ作成
mkdir -p backup/legacy_scripts
cp Assets/Scripts/UI/HUD/HUDManager.cs backup/legacy_scripts/
cp Assets/Scripts/UI/HUD/HealthHUD.cs backup/legacy_scripts/
cp Assets/Scripts/UI/HUD/HUDBase.cs backup/legacy_scripts/

# ファイル削除
rm Assets/Scripts/UI/HUD/HUDManager.cs
rm Assets/Scripts/UI/HUD/HealthHUD.cs
rm Assets/Scripts/UI/HUD/HUDBase.cs
rm Assets/Scripts/UI/HUD/Interfaces/IHUDElement.cs
rm Assets/Scripts/UI/HUD/Interfaces/IHUDManager.cs
rm Assets/Scripts/UI/HUD/Interfaces/IHealthDisplay.cs

# IHealthProvider.cs は保持（MVP システムでも使用）
```

## Phase 4: 名前空間とフォルダ構造の最適化（移行後2ヶ月）

### Step 4.1: フォルダ構造の整理

現在:
```
Assets/Scripts/UI/
├── HUD/
│   └── Interfaces/
│       └── IHealthProvider.cs
└── MVP/
    ├── Interfaces/
    ├── Models/
    ├── Views/
    ├── Presenters/
    ├── Factories/
    └── Managers/
```

最適化後:
```
Assets/Scripts/UI/
├── Interfaces/
│   └── IHealthProvider.cs
├── Models/
├── Views/
├── Presenters/
├── Factories/
└── Managers/
```

### Step 4.2: 名前空間の統一

変更前:
```csharp
namespace Complete.UI.MVP
namespace Complete.UI.HUD
```

変更後:
```csharp
namespace Complete.UI
```

### Step 4.3: ファイル移動とリファクタリング

```bash
# 1. ファイル移動
mv Assets/Scripts/UI/MVP/* Assets/Scripts/UI/
mv Assets/Scripts/UI/HUD/Interfaces/IHealthProvider.cs Assets/Scripts/UI/Interfaces/

# 2. 空フォルダ削除
rmdir Assets/Scripts/UI/MVP
rmdir Assets/Scripts/UI/HUD/Interfaces
rmdir Assets/Scripts/UI/HUD

# 3. 名前空間の一括変更
find Assets/Scripts/UI -name "*.cs" -exec sed -i 's/Complete.UI.MVP/Complete.UI/g' {} \;
find Assets/Scripts/UI -name "*.cs" -exec sed -i 's/Complete.UI.HUD/Complete.UI/g' {} \;
```

## Phase 5: 最終検証（移行後3ヶ月）

### Step 5.1: 完全性チェック

```
□ 旧システムのファイルが完全に削除されている
□ 新システムが正常動作している  
□ パフォーマンス劣化がない
□ メモリリークがない
□ 全テストケースがパス
```

### Step 5.2: ドキュメント更新

```
□ README.md の更新
□ アーキテクチャドキュメントの更新
□ 開発者向けガイドの更新
□ 今回の移行記録の文書化
```

## 注意事項

### 削除前の必須確認事項

1. **コンパイルエラー確認**
   ```
   Unity Editor でコンパイルが成功することを確認
   ```

2. **参照検索**
   ```
   削除対象ファイルが他から参照されていないことを確認
   ```

3. **バックアップ**
   ```
   削除前に必ずバックアップを作成
   ```

4. **段階的実行**
   ```
   一度に全て削除せず、段階的に実行
   ```

### ロールバック手順

問題が発生した場合:

```bash
# バックアップからの復元
cp backup/legacy_scripts/* Assets/Scripts/UI/HUD/

# Git を使用している場合
git checkout HEAD~1 -- Assets/Scripts/UI/HUD/
```

## 削除完了の判定基準

以下の条件を全て満たした場合、削除完了:

```
✓ 旧システムファイルが存在しない
✓ コンパイルエラーなし
✓ 全機能が正常動作
✓ パフォーマンス劣化なし
✓ テストケース全てパス
✓ 1ヶ月以上の安定稼働実績
```

## 移行完了後の利点

削除完了により得られる利点:

1. **コードベースの簡潔性**
   - 重複コードの排除
   - 保守対象ファイルの削減

2. **開発効率の向上**
   - 混乱の原因となる旧システムの排除
   - 一貫したMVPパターンでの開発

3. **パフォーマンス向上**
   - 不要なコンポーネントの排除
   - メモリ使用量の最適化

4. **将来の拡張性**
   - 統一されたアーキテクチャ
   - 新機能追加の容易性