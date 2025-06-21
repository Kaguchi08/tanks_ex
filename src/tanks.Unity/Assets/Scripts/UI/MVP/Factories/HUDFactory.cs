using System;
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
                Debug.LogError("HUDFactory: HealthHUDViewがnullです");
                return null;
            }
            
            try
            {
                var model = new HealthHUDModel();
                var presenter = new HealthHUDPresenter();
                
                presenter.SetView(view);
                presenter.SetHealthModel(model);
                
                await presenter.InitializeAsync();
                
                Debug.Log("HUDFactory: ヘルスHUD作成完了");
                
                return presenter;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HUDFactory: ヘルスHUD作成失敗: {ex}");
                return null;
            }
        }
        
        public async UniTask<IRoundCountPresenter> CreateRoundCountHUDAsync(RoundCountView view)
        {
            if (view == null)
            {
                Debug.LogError("HUDFactory: RoundCountViewがnullです");
                return null;
            }
            
            var model = new RoundCountModel();
            var presenter = new RoundCountPresenter();
            
            presenter.SetView(view);
            presenter.SetRoundCountModel(model);
            
            await presenter.InitializeAsync();
            
            Debug.Log("HUDFactory: ラウンドカウントHUD作成完了");
            
            return presenter;
        }
        
        public async UniTask<IGameTimerPresenter> CreateGameTimerHUDAsync(GameTimerView view)
        {
            
            if (view == null)
            {
                Debug.LogError("HUDFactory: GameTimerViewがnullです");
                return null;
            }
            
            try
            {
                var model = new GameTimerModel();
                var presenter = new GameTimerPresenter();
                
                presenter.SetView(view);
                presenter.SetGameTimerModel(model);
                
                await presenter.InitializeAsync();
                
                Debug.Log("HUDFactory: ゲームタイマーHUD作成完了");
                
                return presenter;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HUDFactory: ゲームタイマーHUD作成失敗: {ex}");
                return null;
            }
        }
    }
}