using System;
using UniRx;

namespace Complete.UI.HUD
{
    public interface IHealthProvider
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        IObservable<float> OnHealthChanged { get; }
        IObservable<Unit> OnDeathEvent { get; }
    }
}