using System;

namespace Complete.UI.HUD
{
    public interface IHealthDisplay
    {
        void UpdateHealth(float currentHealth, float maxHealth);
        IObservable<float> OnHealthCritical { get; }
    }
}