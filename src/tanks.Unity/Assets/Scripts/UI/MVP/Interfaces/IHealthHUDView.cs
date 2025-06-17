using System;
using UniRx;

namespace Complete.UI.MVP
{
    /// <summary>
    /// Health HUD専用のViewインターフェース
    /// HP表示に特化したUI操作を定義
    /// </summary>
    public interface IHealthHUDView : IView
    {
        /// <summary>
        /// HP値を更新
        /// </summary>
        /// <param name="normalizedHealth">正規化されたHP値（0.0 - 1.0）</param>
        void UpdateHealthValue(float normalizedHealth);
        
        /// <summary>
        /// HP色を更新
        /// </summary>
        /// <param name="color">表示色</param>
        void UpdateHealthColor(UnityEngine.Color color);
        
        /// <summary>
        /// クリティカル状態の視覚効果
        /// </summary>
        /// <param name="isCritical">クリティカル状態かどうか</param>
        void SetCriticalState(bool isCritical);
        
        /// <summary>
        /// 死亡状態の視覚効果
        /// </summary>
        void SetDeathState();
    }
}