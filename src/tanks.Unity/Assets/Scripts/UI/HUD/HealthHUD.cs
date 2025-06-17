using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Cysharp.Threading.Tasks;
using System;
using Complete.UI.HUD;

namespace Complete
{
    public class HealthHUD : HUDBase, IHealthDisplay
    {
        [SerializeField] private Slider m_HealthSlider;
        [SerializeField] private Image m_FillImage;
        [SerializeField] private Color m_FullHealthColor = Color.green;
        [SerializeField] private Color m_ZeroHealthColor = Color.red;
        [SerializeField] private Color m_CriticalHealthColor = Color.red;
        [SerializeField] private float m_CriticalHealthThreshold = 0.25f;
        
        private IHealthProvider _healthProvider;
        private readonly Subject<float> _onHealthCriticalSubject = new Subject<float>();
        
        public IObservable<float> OnHealthCritical => _onHealthCriticalSubject.AsObservable();
        
        protected override void Awake()
        {
            base.Awake();
            
            // 同期的な初期化
            if (m_HealthSlider == null)
            {
                m_HealthSlider = GetComponentInChildren<Slider>();
            }
            
            if (m_HealthSlider == null)
            {
                Debug.LogError("HealthHUD: HealthSlider is not assigned and not found in children!", this);
                return;
            }
            
            if (m_FillImage == null && m_HealthSlider.fillRect != null)
            {
                m_FillImage = m_HealthSlider.fillRect.GetComponent<Image>();
            }
            
            Debug.Log($"HealthHUD Awake completed. Slider: {m_HealthSlider?.name}, FillImage: {m_FillImage?.name}");
        }
        
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            await UniTask.DelayFrame(1);
        }
        
        public void SetHealthProvider(IHealthProvider healthProvider)
        {
            UnsubscribeFromCurrentProvider();
            
            _healthProvider = healthProvider;
            
            if (_healthProvider != null)
            {
                SubscribeToHealthChanges();
                
                // 初期値を即座に設定
                UpdateHealth(_healthProvider.CurrentHealth, _healthProvider.MaxHealth);
            }
            else
            {
                Debug.LogWarning("HealthHUD: HealthProvider is null");
            }
        }
        
        
        private void UnsubscribeFromCurrentProvider()
        {
            // CompositeDisposableをクリアして既存の購読を解除
            // base classの_disposablesを使用
        }
        
        private void SubscribeToHealthChanges()
        {
            if (_healthProvider == null) 
            {
                Debug.LogError("HealthHUD: _healthProvider is null");
                return;
            }
            
            try
            {
                _healthProvider.OnHealthChanged
                    .Subscribe(health => UpdateHealth(health, _healthProvider.MaxHealth), 
                    onError: ex => Debug.LogError($"HealthHUD OnHealthChanged error: {ex}"))
                    .AddTo(_disposables);
                    
                _healthProvider.OnDeathEvent
                    .Subscribe(_ => OnPlayerDeath())
                    .AddTo(_disposables);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"HealthHUD error subscribing to health changes: {ex}");
            }
        }
        
        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (m_HealthSlider == null) 
            {
                Debug.LogError("HealthHUD: m_HealthSlider is null!");
                return;
            }
            
            float normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);
            
            m_HealthSlider.value = normalizedHealth;
            
            if (m_FillImage != null)
            {
                m_FillImage.color = GetHealthColor(normalizedHealth);
            }
            
            // Critical health notification
            if (normalizedHealth <= m_CriticalHealthThreshold && normalizedHealth > 0)
            {
                _onHealthCriticalSubject.OnNext(normalizedHealth);
            }
        }
        
        private Color GetHealthColor(float normalizedHealth)
        {
            if (normalizedHealth <= m_CriticalHealthThreshold)
            {
                return Color.Lerp(m_CriticalHealthColor, m_ZeroHealthColor, 
                    1f - (normalizedHealth / m_CriticalHealthThreshold));
            }
            
            return Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, normalizedHealth);
        }
        
        private void OnPlayerDeath()
        {
            if (m_HealthSlider != null)
            {
                m_HealthSlider.value = 0f;
            }
            
            if (m_FillImage != null)
            {
                m_FillImage.color = m_ZeroHealthColor;
            }
        }
        
        public override void Dispose()
        {
            _onHealthCriticalSubject?.Dispose();
            base.Dispose();
        }
    }
}