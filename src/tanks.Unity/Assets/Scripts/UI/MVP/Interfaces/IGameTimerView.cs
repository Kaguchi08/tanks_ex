namespace Complete.UI.MVP
{
    /// <summary>
    /// ゲーム時間HUD専用のViewインターフェース
    /// ラウンド経過時間の表示に特化したUI操作を定義
    /// </summary>
    public interface IGameTimerView : IView
    {
        /// <summary>
        /// 現在のラウンド経過時間を更新
        /// </summary>
        /// <param name="timeInSeconds">経過時間（秒）</param>
        void UpdateRoundTime(float timeInSeconds);
        
        /// <summary>
        /// タイマーの動作状態を設定
        /// </summary>
        /// <param name="isRunning">動作中かどうか</param>
        void SetRunningState(bool isRunning);
    }
}