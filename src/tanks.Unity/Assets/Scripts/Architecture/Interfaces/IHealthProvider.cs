using System;
using UniRx;

namespace Complete.Interfaces
{
    /// <summary>
    /// 体力情報を提供するインターフェース
    /// TankHealthなどの体力システムが実装する
    /// </summary>
    public interface IHealthProvider
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        IObservable<float> OnHealthChanged { get; }
        IObservable<Unit> OnDeathEvent { get; }
    }
}