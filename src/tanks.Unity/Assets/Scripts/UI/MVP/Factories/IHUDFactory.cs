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
    }
}