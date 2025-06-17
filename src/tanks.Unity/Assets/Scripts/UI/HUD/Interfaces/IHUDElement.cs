using System;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Complete.UI.HUD
{
    public interface IHUDElement : IDisposable
    {
        bool IsActive { get; }
        UniTask InitializeAsync();
        void Show();
        void Hide();
        void SetActive(bool active);
        IObservable<Unit> OnShow { get; }
        IObservable<Unit> OnHide { get; }
    }
}