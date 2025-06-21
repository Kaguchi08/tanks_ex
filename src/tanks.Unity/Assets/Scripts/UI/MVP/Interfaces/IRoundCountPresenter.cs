namespace Complete.UI.MVP
{
    /// <summary>
    /// ラウンド数HUD専用のPresenterインターフェース
    /// ラウンド数Modelとラウンド数HUD Viewの仲介を担当
    /// </summary>
    public interface IRoundCountPresenter : IPresenter
    {
        /// <summary>
        /// ラウンド数Modelを設定
        /// </summary>
        /// <param name="roundCountModel">ラウンド数情報を提供するModel</param>
        void SetRoundCountModel(IRoundCountModel roundCountModel);
        
        /// <summary>
        /// ラウンド数HUD Viewを設定
        /// </summary>
        /// <param name="view">ラウンド数表示を担当するView</param>
        void SetView(IRoundCountView view);
    }
}