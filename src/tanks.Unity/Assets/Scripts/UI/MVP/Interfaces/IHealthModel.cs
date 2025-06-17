using System;
using UniRx;

namespace Complete.UI.MVP
{
    /// <summary>
    /// Health Model専用のインターフェース
    /// HP関連のデータとビジネスロジックを定義
    /// </summary>
    public interface IHealthModel : IModel
    {
        /// <summary>
        /// 現在のHP値
        /// </summary>
        float CurrentHealth { get; }
        
        /// <summary>
        /// 最大HP値
        /// </summary>
        float MaxHealth { get; }
        
        /// <summary>
        /// 正規化されたHP値（0.0 - 1.0）
        /// </summary>
        float NormalizedHealth { get; }
        
        /// <summary>
        /// クリティカル状態かどうか
        /// </summary>
        bool IsCritical { get; }
        
        /// <summary>
        /// 死亡状態かどうか
        /// </summary>
        bool IsDead { get; }
        
        /// <summary>
        /// HP変更時の通知
        /// </summary>
        IObservable<float> OnHealthChanged { get; }
        
        /// <summary>
        /// クリティカル状態変更時の通知
        /// </summary>
        IObservable<bool> OnCriticalStateChanged { get; }
        
        /// <summary>
        /// 死亡時の通知
        /// </summary>
        IObservable<Unit> OnDeath { get; }
    }
}