using System;
using UniRx;
using UnityEngine;
using Complete.Interfaces;

namespace Complete.UI.MVP
{
    /// <summary>
    /// Health HUD用のModelクラス
    /// TankHealthからのデータを変換・加工してPresenterに提供
    /// </summary>
    public class HealthHUDModel : IHealthModel
    {
        private float _criticalThreshold = 0.25f;
        
        private IHealthProvider _healthProvider;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<Unit> _onChangedSubject = new Subject<Unit>();
        private readonly Subject<bool> _onCriticalStateChangedSubject = new Subject<bool>();
        
        private bool _wasCritical = false;
        
        // IModel implementation
        public IObservable<Unit> OnChanged => _onChangedSubject.AsObservable();
        
        // IHealthModel implementation
        public float CurrentHealth => _healthProvider?.CurrentHealth ?? 0f;
        public float MaxHealth => _healthProvider?.MaxHealth ?? 100f;
        public float NormalizedHealth => MaxHealth > 0 ? Mathf.Clamp01(CurrentHealth / MaxHealth) : 0f;
        public bool IsCritical => NormalizedHealth <= _criticalThreshold && !IsDead;
        public bool IsDead => CurrentHealth <= 0f;
        
        public IObservable<float> OnHealthChanged => _healthProvider?.OnHealthChanged ?? Observable.Empty<float>();
        public IObservable<bool> OnCriticalStateChanged => _onCriticalStateChangedSubject.AsObservable();
        public IObservable<Unit> OnDeath => _healthProvider?.OnDeathEvent ?? Observable.Empty<Unit>();
        
        /// <summary>
        /// クリティカル閾値を設定
        /// </summary>
        public void SetCriticalThreshold(float threshold)
        {
            _criticalThreshold = Mathf.Clamp01(threshold);
        }
        
        /// <summary>
        /// Health Providerを設定
        /// </summary>
        public void SetHealthProvider(IHealthProvider healthProvider)
        {
            Debug.Log($"HealthHUDModel: SetHealthProvider called - {healthProvider?.GetType().Name}");
            
            UnsubscribeFromCurrentProvider();
            
            _healthProvider = healthProvider;
            
            if (_healthProvider != null)
            {
                Debug.Log($"HealthHUDModel: Health provider set - Current: {_healthProvider.CurrentHealth}, Max: {_healthProvider.MaxHealth}");
                SubscribeToHealthProvider();
                
                // 初期状態をすぐに通知
                Debug.Log("HealthHUDModel: Triggering initial OnChanged notification");
                _onChangedSubject.OnNext(Unit.Default);
            }
            else
            {
                Debug.LogWarning("HealthHUDModel: Health provider is null");
            }
        }
        
        private void UnsubscribeFromCurrentProvider()
        {
            _disposables.Clear();
            _wasCritical = false;
        }
        
        private void SubscribeToHealthProvider()
        {
            if (_healthProvider == null) return;
            
            Debug.Log("HealthHUDModel: Subscribing to health provider events");
            
            // HP変更時の処理
            _healthProvider.OnHealthChanged
                .Subscribe(newHealth => 
                {
                    Debug.Log($"HealthHUDModel: OnHealthChanged received - New Health: {newHealth}");
                    CheckCriticalStateChange();
                    _onChangedSubject.OnNext(Unit.Default);
                })
                .AddTo(_disposables);
                
            // 死亡時の処理
            _healthProvider.OnDeathEvent
                .Subscribe(_ => 
                {
                    CheckCriticalStateChange();
                    _onChangedSubject.OnNext(Unit.Default);
                })
                .AddTo(_disposables);
                
            // 初期状態をチェック
            CheckCriticalStateChange();
        }
        
        private void CheckCriticalStateChange()
        {
            bool currentCritical = IsCritical;
            if (currentCritical != _wasCritical)
            {
                _wasCritical = currentCritical;
                _onCriticalStateChangedSubject.OnNext(currentCritical);
            }
        }
        
        public void Dispose()
        {
            _disposables?.Dispose();
            _onChangedSubject?.Dispose();
            _onCriticalStateChangedSubject?.Dispose();
        }
    }
}