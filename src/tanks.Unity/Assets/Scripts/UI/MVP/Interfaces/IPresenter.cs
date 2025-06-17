using System;
using Cysharp.Threading.Tasks;

namespace Complete.UI.MVP
{
    /// <summary>
    /// MVPパターンのPresenterインターフェース
    /// ModelとViewの仲介役を担当
    /// </summary>
    public interface IPresenter : IDisposable
    {
        /// <summary>
        /// Presenterの初期化
        /// </summary>
        UniTask InitializeAsync();
        
        /// <summary>
        /// Presenterの開始（ViewとModelの接続）
        /// </summary>
        void Start();
        
        /// <summary>
        /// Presenterの停止
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Presenterのアクティブ状態
        /// </summary>
        bool IsActive { get; }
    }
}