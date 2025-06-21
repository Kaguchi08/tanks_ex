namespace Complete.UI.MVP
{
    /// <summary>
    /// ラウンド数HUD専用のViewインターフェース
    /// ラウンド数表示に特化したUI操作を定義
    /// </summary>
    public interface IRoundCountView : IView
    {
        /// <summary>
        /// 現在のラウンド数を更新
        /// </summary>
        /// <param name="currentRound">現在のラウンド数</param>
        void UpdateCurrentRound(int currentRound);
        
        /// <summary>
        /// Player1の勝利数を更新
        /// </summary>
        /// <param name="wins">勝利数</param>
        void UpdatePlayer1Wins(int wins);
        
        /// <summary>
        /// Player2の勝利数を更新
        /// </summary>
        /// <param name="wins">勝利数</param>
        void UpdatePlayer2Wins(int wins);
    }
}