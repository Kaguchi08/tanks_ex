using System;
using UniRx;

namespace Complete.UI.MVP
{
    /// <summary>
    /// ゲーム経過時間Model専用のインターフェース
    /// 時間関連のデータとビジネスロジックを定義
    /// </summary>
    public interface IGameTimerModel : IModel
    {
        /// <summary>
        /// 現在のラウンド経過時間（秒）
        /// </summary>
        float CurrentRoundTime { get; }
        
        /// <summary>
        /// 総ゲーム時間（秒）
        /// </summary>
        float TotalGameTime { get; }
        
        /// <summary>
        /// タイマーが動作中かどうか
        /// </summary>
        bool IsRunning { get; }
        
        /// <summary>
        /// ラウンド時間変更時の通知
        /// </summary>
        IObservable<float> OnRoundTimeChanged { get; }
        
        /// <summary>
        /// 総ゲーム時間変更時の通知
        /// </summary>
        IObservable<float> OnTotalTimeChanged { get; }
        
        /// <summary>
        /// タイマー開始/停止状態変更時の通知
        /// </summary>
        IObservable<bool> OnRunningStateChanged { get; }
    }
}