using System;
using UniRx;

namespace Complete.UI.MVP
{
    /// <summary>
    /// MVPパターンのModelインターフェース
    /// データとビジネスロジックを担当
    /// </summary>
    public interface IModel : IDisposable
    {
        /// <summary>
        /// Modelが変更された時の通知
        /// </summary>
        IObservable<Unit> OnChanged { get; }
    }
}