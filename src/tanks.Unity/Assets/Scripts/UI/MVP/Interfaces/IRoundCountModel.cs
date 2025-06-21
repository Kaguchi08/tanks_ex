using System;
using UniRx;

namespace Complete.UI.MVP
{
    /// <summary>
    /// ラウンド数Model専用のインターフェース
    /// ラウンド関連のデータとビジネスロジックを定義
    /// </summary>
    public interface IRoundCountModel : IModel
    {
        /// <summary>
        /// 現在のラウンド数
        /// </summary>
        int CurrentRound { get; }
        
        /// <summary>
        /// 勝利に必要なラウンド数
        /// </summary>
        int RoundsToWin { get; }
        
        /// <summary>
        /// Player1の勝利数
        /// </summary>
        int Player1Wins { get; }
        
        /// <summary>
        /// Player2の勝利数
        /// </summary>
        int Player2Wins { get; }
        
        /// <summary>
        /// ラウンド数変更時の通知
        /// </summary>
        IObservable<int> OnRoundChanged { get; }
        
        /// <summary>
        /// Player1勝利数変更時の通知
        /// </summary>
        IObservable<int> OnPlayer1WinsChanged { get; }
        
        /// <summary>
        /// Player2勝利数変更時の通知
        /// </summary>
        IObservable<int> OnPlayer2WinsChanged { get; }
    }
}