using Cysharp.Threading.Tasks;

namespace Complete.UI.MVP
{
    /// <summary>
    /// HUD要素のファクトリーインターフェース
    /// MVP構成要素の生成を担当
    /// </summary>
    public interface IHUDFactory
    {
        /// <summary>
        /// Health HUDのMVP構成要素を作成
        /// </summary>
        /// <param name="view">ViewコンポーネントのGameObject</param>
        /// <returns>作成されたPresenter</returns>
        UniTask<IHealthHUDPresenter> CreateHealthHUDAsync(HealthHUDView view);
        
        /// <summary>
        /// ラウンド数HUDのMVP構成要素を作成
        /// </summary>
        /// <param name="view">ViewコンポーネントのGameObject</param>
        /// <returns>作成されたPresenter</returns>
        UniTask<IRoundCountPresenter> CreateRoundCountHUDAsync(RoundCountView view);
        
        /// <summary>
        /// ゲーム時間HUDのMVP構成要素を作成
        /// </summary>
        /// <param name="view">ViewコンポーネントのGameObject</param>
        /// <returns>作成されたPresenter</returns>
        UniTask<IGameTimerPresenter> CreateGameTimerHUDAsync(GameTimerView view);
    }
}