using System;
using Cysharp.Threading.Tasks;

namespace Complete.UI.MVP
{
    /// <summary>
    /// MVPパターンのViewインターフェース
    /// UI表示とユーザー入力を担当
    /// </summary>
    public interface IView : IDisposable
    {
        /// <summary>
        /// Viewの初期化
        /// </summary>
        UniTask InitializeAsync();
        
        /// <summary>
        /// Viewの表示
        /// </summary>
        void Show();
        
        /// <summary>
        /// Viewの非表示
        /// </summary>
        void Hide();
        
        /// <summary>
        /// Viewのアクティブ状態
        /// </summary>
        bool IsActive { get; }
    }
}