using System;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Complete.UI.HUD
{
    public interface IHUDManager : IDisposable
    {
        UniTask InitializeAsync();
        void RegisterHUDElement<T>(T element) where T : IHUDElement;
        void UnregisterHUDElement<T>(T element) where T : IHUDElement;
        T GetHUDElement<T>() where T : class, IHUDElement;
        void ShowAll();
        void HideAll();
        IObservable<bool> OnVisibilityChanged { get; }
    }
}