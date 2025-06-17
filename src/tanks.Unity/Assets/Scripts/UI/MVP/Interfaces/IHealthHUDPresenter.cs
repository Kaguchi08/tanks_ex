namespace Complete.UI.MVP
{
    /// <summary>
    /// Health HUD専用のPresenterインターフェース
    /// Health ModelとHealth HUD Viewの仲介を担当
    /// </summary>
    public interface IHealthHUDPresenter : IPresenter
    {
        /// <summary>
        /// Health Modelを設定
        /// </summary>
        /// <param name="healthModel">HP情報を提供するModel</param>
        void SetHealthModel(IHealthModel healthModel);
        
        /// <summary>
        /// Health HUD Viewを設定
        /// </summary>
        /// <param name="view">HP表示を担当するView</param>
        void SetView(IHealthHUDView view);
    }
}