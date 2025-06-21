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
            
            Debug.Log("HealthHUDPresenter: ヘルスHUDプレゼンター初期化完了");
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
            
            Debug.Log($"HealthHUDPresenter: モデル設定 - {healthModel?.GetType().Name}");
        }
        
        public void SetView(IHealthHUDView view)
        {
            _view = view;
            Debug.Log($"HealthHUDPresenter: ビュー設定 - {view?.GetType().Name}");
        }
        
        public IHealthModel GetModel()
        {
            return _model;
        }
        
        public void Start()
        {
            if (_isActive)
            {
                Debug.LogWarning("HealthHUDPresenter: ヘルスHUDプレゼンターは既にアクティブです");
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
            
            Debug.Log("HealthHUDPresenter: ヘルスHUDプレゼンター開始");
        }
        
        public void Stop()
        {
            if (!_isActive)
            {
                Debug.LogWarning("HealthHUDPresenter: ヘルスHUDプレゼンターは既に非アクティブです");
                return;
            }
            
            _isActive = false;
            UnsubscribeFromModel();
            
            if (_view != null)
            {
                _view.Hide();
            }
            
            Debug.Log("HealthHUDPresenter: ヘルスHUDプレゼンター停止");
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
                
            Debug.Log("HealthHUDPresenter: モデルイベント購読開始");
        }
        
        private void UnsubscribeFromModel()
        {
            _disposables.Clear();
            Debug.Log("HealthHUDPresenter: モデルイベント購読終了");
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
                Debug.LogWarning("HealthHUDPresenter: モデルまたはビューがnullです");
                return;
            }
            
            float normalizedHealth = _model.NormalizedHealth;
            
            Debug.Log($"HealthHUDPresenter: ヘルス更新 - 現在: {_model.CurrentHealth}, 最大: {_model.MaxHealth}, 正規化: {normalizedHealth:P0}");
            
            // HP値を更新
            _view.UpdateHealthValue(normalizedHealth);
            Debug.Log($"HealthHUDPresenter: UpdateHealthValue({normalizedHealth})呼出");
            
            // HP色を更新（死亡状態でなければ）
            if (!_model.IsDead)
            {
                Color healthColor = CalculateHealthColor(normalizedHealth, _model.IsCritical);
                _view.UpdateHealthColor(healthColor);
                Debug.Log($"HealthHUDPresenter: UpdateHealthColor({healthColor})呼出");
            }
            
            Debug.Log($"HealthHUDPresenter: ヘルス更新完了 - {_model.CurrentHealth}/{_model.MaxHealth} ({normalizedHealth:P0})");
        }
        
        private void UpdateCriticalState(bool isCritical)
        {
            if (_view == null) return;
            
            _view.SetCriticalState(isCritical);
            Debug.Log($"HealthHUDPresenter: クリティカル状態更新 - {isCritical}");
        }
        
        private void UpdateDeathState()
        {
            if (_view == null) return;
            
            _view.SetDeathState();
            Debug.Log("HealthHUDPresenter: 死亡状態更新");
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
            
            Debug.Log("HealthHUDPresenter: リソース解放完了");
        }
    }
}