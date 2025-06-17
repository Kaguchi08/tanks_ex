using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using Complete.Interfaces;

namespace Complete.UI.MVP
{
    /// <summary>
    /// MVP構成でのHUD管理クラス
    /// 各HUD要素のPresenterを統括管理
    /// </summary>
    public class MVPHUDManager : MonoBehaviour, IDisposable
    {
        [SerializeField] private Canvas _hudCanvas;
        [SerializeField] private HealthHUDView _healthHUDView;
        
        private IHUDFactory _hudFactory;
        private readonly Dictionary<Type, IPresenter> _presenters = new Dictionary<Type, IPresenter>();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<bool> _onVisibilityChangedSubject = new Subject<bool>();
        
        private bool _isVisible = true;
        private bool _isInitialized = false;
        
        public IObservable<bool> OnVisibilityChanged => _onVisibilityChangedSubject.AsObservable();
        public bool IsInitialized => _isInitialized;
        
        private void Awake()
        {
            _hudFactory = new HUDFactory();
            InitializeSynchronously();
        }
        
        private void InitializeSynchronously()
        {
            // 同期的な初期化処理
            ValidateComponents();
        }
        
        private void Start()
        {
            // 非同期初期化を開始
            _ = InitializeAsync();
        }
        
        private void ValidateComponents()
        {
            if (_hudCanvas == null)
            {
                _hudCanvas = GetComponentInChildren<Canvas>();
                Debug.Log($"MVPHUDManager: Canvas auto-detected - {_hudCanvas?.name}");
            }
            
            if (_healthHUDView == null)
            {
                _healthHUDView = GetComponentInChildren<HealthHUDView>();
                Debug.Log($"MVPHUDManager: HealthHUDView auto-detected - {_healthHUDView?.name}");
            }
        }
        
        public async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("MVPHUDManager is already initialized");
                return;
            }
            
            Debug.Log("=== MVPHUDManager Initialize Start ===");
            
            try
            {
                // Health HUDの初期化
                if (_healthHUDView != null)
                {
                    await InitializeHealthHUDAsync();
                }
                else
                {
                    Debug.LogWarning("MVPHUDManager: HealthHUDView not found");
                }
                
                _isInitialized = true;
                Debug.Log($"MVPHUDManager initialized with {_presenters.Count} presenters");
            }
            catch (Exception ex)
            {
                Debug.LogError($"MVPHUDManager initialization failed: {ex}");
            }
            
            Debug.Log("=== MVPHUDManager Initialize End ===");
        }
        
        private async UniTask InitializeHealthHUDAsync()
        {
            try
            {
                var presenter = await _hudFactory.CreateHealthHUDAsync(_healthHUDView);
                if (presenter != null)
                {
                    RegisterPresenter<IHealthHUDPresenter>(presenter);
                    Debug.Log("MVPHUDManager: Health HUD initialized successfully");
                }
                else
                {
                    Debug.LogError("MVPHUDManager: Failed to create Health HUD presenter");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MVPHUDManager: Health HUD initialization error - {ex}");
            }
        }
        
        public void RegisterPresenter<T>(T presenter) where T : IPresenter
        {
            var type = typeof(T);
            if (!_presenters.ContainsKey(type))
            {
                _presenters[type] = presenter;
                Debug.Log($"MVPHUDManager: Presenter registered - {type.Name}");
            }
            else
            {
                Debug.LogWarning($"MVPHUDManager: Presenter already registered - {type.Name}");
            }
        }
        
        public T GetPresenter<T>() where T : class, IPresenter
        {
            var type = typeof(T);
            if (_presenters.TryGetValue(type, out var presenter))
            {
                return presenter as T;
            }
            
            Debug.LogWarning($"MVPHUDManager: Presenter not found - {type.Name}");
            return null;
        }
        
        public void ShowAll()
        {
            Debug.Log($"MVPHUDManager: ShowAll called - IsVisible: {_isVisible}, Presenter Count: {_presenters.Count}");
            
            // 強制的にキャンバスを有効化
            if (_hudCanvas != null)
            {
                _hudCanvas.gameObject.SetActive(true);
                Debug.Log("MVPHUDManager: HUD Canvas activated");
            }
            
            // 全てのPresenterを強制的に開始
            foreach (var presenter in _presenters.Values)
            {
                if (presenter != null)
                {
                    Debug.Log($"MVPHUDManager: Starting presenter - {presenter.GetType().Name}, Current IsActive: {presenter.IsActive}");
                    if (!presenter.IsActive)
                    {
                        presenter.Start();
                        Debug.Log($"MVPHUDManager: Presenter started - {presenter.GetType().Name}, New IsActive: {presenter.IsActive}");
                    }
                    else
                    {
                        Debug.Log($"MVPHUDManager: Presenter already active - {presenter.GetType().Name}");
                    }
                }
                else
                {
                    Debug.LogWarning("MVPHUDManager: Null presenter found in collection");
                }
            }
            
            _isVisible = true;
            _onVisibilityChangedSubject.OnNext(true);
            Debug.Log("MVPHUDManager: ShowAll completed");
        }
        
        public void HideAll()
        {
            if (_isVisible)
            {
                _isVisible = false;
                
                foreach (var presenter in _presenters.Values)
                {
                    presenter?.Stop();
                }
                
                if (_hudCanvas != null)
                {
                    _hudCanvas.gameObject.SetActive(false);
                }
                
                _onVisibilityChangedSubject.OnNext(false);
                Debug.Log("MVPHUDManager: All HUD elements hidden");
            }
        }
        
        /// <summary>
        /// プレイヤーのHealth Providerを設定
        /// </summary>
        public void SetPlayerHealthProvider(IHealthProvider healthProvider)
        {
            if (healthProvider == null)
            {
                Debug.LogWarning("MVPHUDManager: HealthProvider is null");
                return;
            }
            
            Debug.Log($"MVPHUDManager: Setting health provider - {healthProvider.GetType().Name}");
            Debug.Log($"MVPHUDManager: Current Health: {healthProvider.CurrentHealth}, Max Health: {healthProvider.MaxHealth}");
            Debug.Log($"MVPHUDManager: IsInitialized: {_isInitialized}, Presenter Count: {_presenters.Count}");
            
            var healthPresenter = GetPresenter<IHealthHUDPresenter>();
            if (healthPresenter != null)
            {
                Debug.Log($"MVPHUDManager: Found health presenter - {healthPresenter.GetType().Name}, IsActive: {healthPresenter.IsActive}");
                
                // Presenterを通してModelにHealthProviderを設定
                if (healthPresenter is HealthHUDPresenter concretePresenter)
                {
                    // 既存のModelを取得または新規作成
                    var existingModel = concretePresenter.GetModel();
                    if (existingModel is HealthHUDModel healthModel)
                    {
                        // 既存のModelにHealthProviderを設定
                        Debug.Log("MVPHUDManager: Updating existing model with health provider");
                        healthModel.SetHealthProvider(healthProvider);
                        Debug.Log("MVPHUDManager: Updated existing model with health provider");
                    }
                    else
                    {
                        // 新しいModelを作成
                        Debug.Log("MVPHUDManager: Creating new model with health provider");
                        var model = new HealthHUDModel();
                        model.SetHealthProvider(healthProvider);
                        concretePresenter.SetHealthModel(model);
                        Debug.Log("MVPHUDManager: Created new model with health provider");
                    }
                    
                    // Presenterを強制的に開始（まだ開始されていない場合）
                    if (!concretePresenter.IsActive)
                    {
                        Debug.Log("MVPHUDManager: Force starting presenter after setting health provider");
                        concretePresenter.Start();
                        Debug.Log($"MVPHUDManager: Presenter started, IsActive: {concretePresenter.IsActive}");
                    }
                    
                    Debug.Log("MVPHUDManager: Player health provider set successfully");
                }
                else
                {
                    Debug.LogError("MVPHUDManager: Health presenter is not the expected concrete type");
                }
            }
            else
            {
                Debug.LogError("MVPHUDManager: Health HUD presenter not found");
            }
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        public void Dispose()
        {
            foreach (var presenter in _presenters.Values)
            {
                presenter?.Dispose();
            }
            _presenters.Clear();
            
            _disposables?.Dispose();
            _onVisibilityChangedSubject?.Dispose();
            
            Debug.Log("MVPHUDManager disposed");
        }
        
        /// <summary>
        /// デバッグ用: 全体の状態を診断
        /// </summary>
        [ContextMenu("Diagnose MVP HUD System")]
        public void DiagnoseMVPSystem()
        {
            Debug.Log("=== MVP HUD System Diagnosis ===");
            Debug.Log($"Is Initialized: {_isInitialized}");
            Debug.Log($"Is Visible: {_isVisible}");
            Debug.Log($"Presenter Count: {_presenters.Count}");
            Debug.Log($"HUD Canvas: {(_hudCanvas != null ? _hudCanvas.name : "null")}");
            Debug.Log($"Health HUD View: {(_healthHUDView != null ? _healthHUDView.name : "null")}");
            
            // HealthHUDViewのスライダー確認
            if (_healthHUDView != null)
            {
                _healthHUDView.TestSliderUpdate(0.5f);
            }
            
            // Presenterの状態確認
            foreach (var kvp in _presenters)
            {
                Debug.Log($"Presenter: {kvp.Key.Name} -> {kvp.Value.GetType().Name}, Active: {kvp.Value.IsActive}");
            }
            
            Debug.Log("=== Diagnosis Complete ===");
        }
    }
}