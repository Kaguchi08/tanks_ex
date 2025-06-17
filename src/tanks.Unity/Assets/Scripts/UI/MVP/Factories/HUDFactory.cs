using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Complete.UI.MVP
{
    /// <summary>
    /// HUD要素のファクトリークラス
    /// MVP構成要素を統合的に生成・設定
    /// </summary>
    public class HUDFactory : IHUDFactory
    {
        public async UniTask<IHealthHUDPresenter> CreateHealthHUDAsync(HealthHUDView view)
        {
            if (view == null)
            {
                Debug.LogError("HUDFactory: HealthHUDView is null");
                return null;
            }
            
            // Model作成
            var model = new HealthHUDModel();
            
            // Presenter作成
            var presenter = new HealthHUDPresenter();
            
            // MVP構成要素を接続
            presenter.SetView(view);
            presenter.SetHealthModel(model);
            
            // 初期化
            await presenter.InitializeAsync();
            
            Debug.Log($"HUDFactory: Health HUD MVP created for {view.name}");
            
            return presenter;
        }
    }
}