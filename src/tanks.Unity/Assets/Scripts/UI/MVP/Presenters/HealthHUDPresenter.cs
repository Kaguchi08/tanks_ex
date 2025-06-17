using System;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Complete.UI.MVP
{
    /// <summary>
    /// Health HUD用のPresenterクラス
    /// ModelとViewの仲介を担当し、ビジネスロジックを処理
    /// </summary>
    public class HealthHUDPresenter : IHealthHUDPresenter
    {
        private IHealthModel _model;
        private IHealthHUDView _view;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private bool _isActive = false;
        
        public bool IsActive => _isActive;
        
        public async UniTask InitializeAsync()
        {
            if (_view != null)
            {
                await _view.InitializeAsync();
            }
            
            Debug.Log("HealthHUDPresenter initialized");
        }
        
        public void SetHealthModel(IHealthModel healthModel)
        {
            if (_model != null && _isActive)
            {
                // 既存のModelから購読を解除
                UnsubscribeFromModel();
            }
            
            _model = healthModel;
            
            if (_model != null && _isActive)
            {
                // 新しいModelに購読
                SubscribeToModel();
                // 現在の状態を即座に反映
                UpdateViewFromModel();
            }
            
            Debug.Log($"HealthHUDPresenter: Model set - {healthModel?.GetType().Name}");
        }
        
        public void SetView(IHealthHUDView view)
        {
            _view = view;
            Debug.Log($"HealthHUDPresenter: View set - {view?.GetType().Name}");
        }
        
        public IHealthModel GetModel()
        {
            return _model;
        }
        
        public void Start()
        {
            if (_isActive)
            {
                Debug.LogWarning("HealthHUDPresenter is already active");
                return;
            }
            
            _isActive = true;
            
            if (_model != null)
            {
                SubscribeToModel();
                UpdateViewFromModel();
            }
            
            if (_view != null)
            {
                _view.Show();
            }
            
            Debug.Log("HealthHUDPresenter started");
        }
        
        public void Stop()
        {
            if (!_isActive)
            {
                Debug.LogWarning("HealthHUDPresenter is already inactive");
                return;
            }
            
            _isActive = false;
            UnsubscribeFromModel();
            
            if (_view != null)
            {
                _view.Hide();
            }
            
            Debug.Log("HealthHUDPresenter stopped");
        }
        
        private void SubscribeToModel()
        {
            if (_model == null) return;
            
            // HP変更の監視
            _model.OnHealthChanged
                .Subscribe(_ => UpdateHealthDisplay())
                .AddTo(_disposables);
            
            // クリティカル状態変更の監視
            _model.OnCriticalStateChanged
                .Subscribe(isCritical => UpdateCriticalState(isCritical))
                .AddTo(_disposables);
            
            // 死亡時の監視
            _model.OnDeath
                .Subscribe(_ => UpdateDeathState())
                .AddTo(_disposables);
            
            // モデル変更全般の監視
            _model.OnChanged
                .Subscribe(_ => UpdateViewFromModel())
                .AddTo(_disposables);
                
            Debug.Log("HealthHUDPresenter: Subscribed to model events");
        }
        
        private void UnsubscribeFromModel()
        {
            _disposables.Clear();
            Debug.Log("HealthHUDPresenter: Unsubscribed from model events");
        }
        
        private void UpdateViewFromModel()
        {
            if (_model == null || _view == null) return;
            
            UpdateHealthDisplay();
            UpdateCriticalState(_model.IsCritical);
            
            if (_model.IsDead)
            {
                UpdateDeathState();
            }
        }
        
        private void UpdateHealthDisplay()
        {
            if (_model == null || _view == null) 
            {
                Debug.LogWarning("HealthHUDPresenter: UpdateHealthDisplay called but model or view is null");
                return;
            }
            
            float normalizedHealth = _model.NormalizedHealth;
            
            Debug.Log($"HealthHUDPresenter: UpdateHealthDisplay called - Current: {_model.CurrentHealth}, Max: {_model.MaxHealth}, Normalized: {normalizedHealth:P0}");
            
            // HP値を更新
            _view.UpdateHealthValue(normalizedHealth);
            Debug.Log($"HealthHUDPresenter: Called UpdateHealthValue({normalizedHealth})");
            
            // HP色を更新（死亡状態でなければ）
            if (!_model.IsDead)
            {
                Color healthColor = CalculateHealthColor(normalizedHealth, _model.IsCritical);
                _view.UpdateHealthColor(healthColor);
                Debug.Log($"HealthHUDPresenter: Called UpdateHealthColor({healthColor})");
            }
            
            Debug.Log($"HealthHUDPresenter: Health updated - {_model.CurrentHealth}/{_model.MaxHealth} ({normalizedHealth:P0})");
        }
        
        private void UpdateCriticalState(bool isCritical)
        {
            if (_view == null) return;
            
            _view.SetCriticalState(isCritical);
            Debug.Log($"HealthHUDPresenter: Critical state updated - {isCritical}");
        }
        
        private void UpdateDeathState()
        {
            if (_view == null) return;
            
            _view.SetDeathState();
            Debug.Log("HealthHUDPresenter: Death state updated");
        }
        
        private Color CalculateHealthColor(float normalizedHealth, bool isCritical)
        {
            // ViewのGetHealthColorメソッドを使用するか、独自のロジックを実装
            if (_view is HealthHUDView healthView)
            {
                return healthView.GetHealthColor(normalizedHealth);
            }
            
            // フォールバック: 基本的な色計算
            if (normalizedHealth <= 0f)
                return Color.red;
            else if (isCritical)
                return Color.Lerp(Color.red, Color.yellow, normalizedHealth / 0.25f);
            else
                return Color.Lerp(Color.yellow, Color.green, (normalizedHealth - 0.25f) / 0.75f);
        }
        
        public void Dispose()
        {
            Stop();
            _disposables?.Dispose();
            _model?.Dispose();
            _view?.Dispose();
            
            Debug.Log("HealthHUDPresenter disposed");
        }
    }
}