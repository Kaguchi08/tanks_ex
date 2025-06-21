namespace Complete.UI.MVP
{
    /// <summary>
    /// ゲーム時間HUD専用のPresenterインターフェース
    /// ゲーム時間Modelとゲーム時間HUD Viewの仲介を担当
    /// </summary>
    public interface IGameTimerPresenter : IPresenter
    {
        /// <summary>
        /// ゲーム時間Modelを設定
        /// </summary>
        /// <param name="gameTimerModel">時間情報を提供するModel</param>
        void SetGameTimerModel(IGameTimerModel gameTimerModel);
        
        /// <summary>
        /// ゲーム時間HUD Viewを設定
        /// </summary>
        /// <param name="view">時間表示を担当するView</param>
        void SetView(IGameTimerView view);
    }
}