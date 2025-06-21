using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        [SerializeField] private RoundCountView _roundCountHUDView;
        [SerializeField] private GameTimerView _gameTimerHUDView;
        
        private IHUDFactory _hudFactory;
        private readonly Dictionary<Type, IPresenter> _presenters = new Dictionary<Type, IPresenter>();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<bool> _onVisibilityChangedSubject = new Subject<bool>();
        private readonly Queue<System.Action> _pendingActions = new Queue<System.Action>();
        private TaskCompletionSource<bool> _initializationCompletionSource;
        
        private bool _isVisible = true;
        private bool _isInitialized = false;
        
        public IObservable<bool> OnVisibilityChanged => _onVisibilityChangedSubject.AsObservable();
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// 初期化完了を待機する
        /// </summary>
        public async UniTask WaitForInitializationAsync()
        {
            if (_isInitialized)
            {
                return;
            }
            
            await _initializationCompletionSource.Task;
        }
        
        /// <summary>
        /// 保留されていたアクションを処理
        /// </summary>
        private void ProcessPendingActions()
        {
            int actionCount = _pendingActions.Count;
            if (actionCount > 0)
            {
                Debug.Log($"MVPHUDManager: 保留アクション実行: {actionCount}件");
            }
            
            while (_pendingActions.Count > 0)
            {
                var action = _pendingActions.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"MVPHUDManager: 保留アクション実行エラー: {ex}");
                }
            }
            
        }
        
        private void Awake()
        {
            // Unity Editor対応: シーン変更時にMVPHUDManagerが破棄されないよう保護
            #if UNITY_EDITOR
            // 既存のインスタンスがあるかチェック
            var existingInstance = FindObjectOfType<MVPHUDManager>();
            if (existingInstance != null && existingInstance != this)
            {
                Debug.LogWarning($"MVPHUDManager: 重複インスタンス検出、破棄: {gameObject.name}");
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);
            #endif
            
            _hudFactory = new HUDFactory();
            _initializationCompletionSource = new TaskCompletionSource<bool>();
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
                _hudCanvas = GetComponentInChildren<Canvas>();
            
            if (_healthHUDView == null)
                _healthHUDView = GetComponentInChildren<HealthHUDView>();
            
            if (_roundCountHUDView == null)
                _roundCountHUDView = GetComponentInChildren<RoundCountView>();
            
            if (_gameTimerHUDView == null)
                _gameTimerHUDView = GetComponentInChildren<GameTimerView>();
        }
        
        public async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }
            
            try
            {
                // Health HUDの初期化
                if (_healthHUDView != null)
                {
                    await InitializeHealthHUDAsync();
                }
                else
                {
                    Debug.LogWarning("MVPHUDManager: HealthHUDViewが見つかりません");
                }
                
                // Round Count HUDの初期化
                if (_roundCountHUDView != null)
                {
                    await InitializeRoundCountHUDAsync();
                }
                else
                {
                    Debug.LogWarning("MVPHUDManager: RoundCountViewが見つかりません");
                }
                
                // Game Timer HUDの初期化
                if (_gameTimerHUDView != null)
                {
                    await InitializeGameTimerHUDAsync();
                }
                else
                {
                    Debug.LogWarning("MVPHUDManager: GameTimerViewが見つかりません");
                }
                
                _isInitialized = true;
                Debug.Log($"MVPHUDManager: HUD初期化完了 - Presenter数: {_presenters.Count}");
                
                // 保留されていたアクションを実行
                ProcessPendingActions();
                
                // 初期化完了を通知
                _initializationCompletionSource.SetResult(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"MVPHUDManager: HUD初期化失敗: {ex}");
                _initializationCompletionSource.SetException(ex);
            }
            
        }
        
        private async UniTask InitializeHealthHUDAsync()
        {
            try
            {
                var presenter = await _hudFactory.CreateHealthHUDAsync(_healthHUDView);
                if (presenter != null)
                {
                    RegisterPresenter<IHealthHUDPresenter>(presenter);
                    Debug.Log("MVPHUDManager: ヘルスHUD初期化完了");
                }
                else
                {
                    Debug.LogError("MVPHUDManager: ヘルスHUDプレゼンター作成失敗");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MVPHUDManager: ヘルスHUD初期化エラー: {ex}");
            }
        }
        
        private async UniTask InitializeRoundCountHUDAsync()
        {
            try
            {
                var presenter = await _hudFactory.CreateRoundCountHUDAsync(_roundCountHUDView);
                if (presenter != null)
                {
                    RegisterPresenter<IRoundCountPresenter>(presenter);
                    Debug.Log("MVPHUDManager: ラウンドカウントHUD初期化完了");
                }
                else
                {
                    Debug.LogError("MVPHUDManager: ラウンドカウントHUDプレゼンター作成失敗");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MVPHUDManager: ラウンドカウントHUD初期化エラー: {ex}");
            }
        }
        
        private async UniTask InitializeGameTimerHUDAsync()
        {
            try
            {
                var presenter = await _hudFactory.CreateGameTimerHUDAsync(_gameTimerHUDView);
                if (presenter != null)
                {
                    RegisterPresenter<IGameTimerPresenter>(presenter);
                    Debug.Log("MVPHUDManager: ゲームタイマーHUD初期化完了");
                }
                else
                {
                    Debug.LogError("MVPHUDManager: ゲームタイマーHUDプレゼンター作成失敗");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MVPHUDManager: ゲームタイマーHUD初期化エラー: {ex}");
            }
        }
        
        public void RegisterPresenter<T>(T presenter) where T : IPresenter
        {
            var type = typeof(T);
            if (!_presenters.ContainsKey(type))
            {
                _presenters[type] = presenter;
            }
            else
            {
                Debug.LogWarning($"MVPHUDManager: プレゼンター重複登録: {type.Name}");
            }
        }
        
        public T GetPresenter<T>() where T : class, IPresenter
        {
            var type = typeof(T);
            if (_presenters.TryGetValue(type, out var presenter))
            {
                return presenter as T;
            }
            
            Debug.LogWarning($"MVPHUDManager: プレゼンターが見つかりません: {type.Name}");
            return null;
        }
        
        public void ShowAll()
        {
            
            // 強制的にキャンバスを有効化
            if (_hudCanvas != null)
            {
                _hudCanvas.gameObject.SetActive(true);
            }
            
            // 全てのPresenterを強制的に開始
            foreach (var presenter in _presenters.Values)
            {
                if (presenter != null)
                {
                    if (!presenter.IsActive)
                    {
                        presenter.Start();
                    }
                }
                else
                {
                    Debug.LogWarning("MVPHUDManager: nullプレゼンターを検出");
                }
            }
            
            _isVisible = true;
            _onVisibilityChangedSubject.OnNext(true);
            Debug.Log("MVPHUDManager: 全HUD表示完了");
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
                Debug.Log("MVPHUDManager: 全HUD非表示完了");
            }
        }
        
        /// <summary>
        /// ゲーム状態をラウンド数HUDに同期
        /// </summary>
        public void SyncGameStateToRoundCountHUD(GameManager gameManager)
        {
            
            if (gameManager == null)
            {
                Debug.LogWarning("MVPHUDManager: GameManagerがnullです");
                return;
            }
            
            if (!_isInitialized)
            {
                Debug.LogWarning("MVPHUDManager: 未初期化のためゲーム状態を同期できません");
                return;
            }
            
            var roundCountPresenter = GetPresenter<IRoundCountPresenter>();
            
            if (roundCountPresenter is RoundCountPresenter concretePresenter)
            {
                var model = concretePresenter.GetModel() as RoundCountModel;
                
                if (model != null)
                {
                    model.SyncFromGameManager(gameManager);
                    model.SyncWinsFromTankManagers(gameManager.m_Tanks);
                    Debug.Log("MVPHUDManager: ラウンドカウントHUDのゲーム状態同期完了");
                }
                else
                {
                    Debug.LogError("MVPHUDManager: RoundCountModelがnullです");
                }
            }
            else
            {
                Debug.LogError("MVPHUDManager: RoundCountPresenterが見つからないか型が違います");
            }
        }
        
        /// <summary>
        /// ゲームタイマーを開始/停止
        /// </summary>
        public void SetGameTimerRunning(bool isRunning)
        {
            
            if (!_isInitialized)
            {
                Debug.LogWarning("MVPHUDManager: 未初期化のためタイマー状態を設定できません");
                return;
            }
            
            var timerPresenter = GetPresenter<IGameTimerPresenter>();
            
            if (timerPresenter is GameTimerPresenter concretePresenter)
            {
                var model = concretePresenter.GetModel() as GameTimerModel;
                
                if (model != null)
                {
                    if (isRunning)
                    {
                        model.StartTimer();
                    }
                    else
                    {
                        model.StopTimer();
                    }
                    Debug.Log($"MVPHUDManager: ゲームタイマー{(isRunning ? "開始" : "停止")}");
                }
                else
                {
                    Debug.LogError("MVPHUDManager: GameTimerModelがnullです");
                }
            }
            else
            {
                Debug.LogError("MVPHUDManager: GameTimerPresenterが見つからないか型が違います");
            }
        }
        
        /// <summary>
        /// 後から接続したクライアント用：進行中のラウンド時間を同期
        /// </summary>
        public void SyncRoundTime(int roundNumber, float currentRoundTime)
        {
            // ラウンド数を更新
            var roundCountPresenter = GetPresenter<IRoundCountPresenter>();
            if (roundCountPresenter is RoundCountPresenter concreteRoundPresenter)
            {
                var roundModel = concreteRoundPresenter.GetModel() as RoundCountModel;
                if (roundModel != null)
                {
                    roundModel.UpdateCurrentRound(roundNumber);
                }
            }
            
            // 進行中のラウンド時間を同期
            var timerPresenter = GetPresenter<IGameTimerPresenter>();
            if (timerPresenter is GameTimerPresenter concreteTimerPresenter)
            {
                var timerModel = concreteTimerPresenter.GetModel() as GameTimerModel;
                if (timerModel != null)
                {
                    timerModel.SyncRoundTime(currentRoundTime, true);
                }
            }
            
            Debug.Log($"MVPHUDManager: ラウンド{roundNumber}の時間{currentRoundTime:F1}秒で同期完了");
        }
        
        /// <summary>
        /// 新しいラウンドを開始
        /// </summary>
        public void StartNewRound(int roundNumber)
        {
            
            if (!_isInitialized)
            {
                Debug.LogWarning("MVPHUDManager: 未初期化のため新ラウンドを開始できません");
                return;
            }
            
            // ラウンド数を更新
            var roundCountPresenter = GetPresenter<IRoundCountPresenter>();
            
            if (roundCountPresenter is RoundCountPresenter concreteRoundPresenter)
            {
                var roundModel = concreteRoundPresenter.GetModel() as RoundCountModel;
                
                if (roundModel != null)
                {
                    roundModel.UpdateCurrentRound(roundNumber);
                }
            }
            
            // ラウンド時間をリセット
            var timerPresenter = GetPresenter<IGameTimerPresenter>();
            
            if (timerPresenter is GameTimerPresenter concreteTimerPresenter)
            {
                var timerModel = concreteTimerPresenter.GetModel() as GameTimerModel;
                
                if (timerModel != null)
                {
                    timerModel.StartNewRound();
                }
            }
            
            Debug.Log($"MVPHUDManager: 新ラウンド{roundNumber}開始");
        }
        
        /// <summary>
        /// プレイヤーのHealth Providerを設定
        /// </summary>
        public void SetPlayerHealthProvider(IHealthProvider healthProvider)
        {
            if (healthProvider == null)
            {
                Debug.LogWarning("MVPHUDManager: HealthProviderがnullです");
                return;
            }
            
            
            // 初期化が完了していない場合はアクションをキューに追加
            if (!_isInitialized)
            {
                Debug.Log("MVPHUDManager: 初期化待ちのためヘルスプロバイダー設定を保留");
                _pendingActions.Enqueue(() => SetPlayerHealthProviderInternal(healthProvider));
                return;
            }
            
            SetPlayerHealthProviderInternal(healthProvider);
        }
        
        /// <summary>
        /// プレイヤーのHealth Provider設定の内部実装
        /// </summary>
        private void SetPlayerHealthProviderInternal(IHealthProvider healthProvider)
        {
            
            var healthPresenter = GetPresenter<IHealthHUDPresenter>();
            if (healthPresenter != null)
            {
                
                // Presenterを通してModelにHealthProviderを設定
                if (healthPresenter is HealthHUDPresenter concretePresenter)
                {
                    // 既存のModelを取得または新規作成
                    var existingModel = concretePresenter.GetModel();
                    if (existingModel is HealthHUDModel healthModel)
                    {
                        // 既存のModelにHealthProviderを設定
                        healthModel.SetHealthProvider(healthProvider);
                    }
                    else
                    {
                        // 新しいModelを作成
                        var model = new HealthHUDModel();
                        model.SetHealthProvider(healthProvider);
                        concretePresenter.SetHealthModel(model);
                    }
                    
                    // Presenterを強制的に開始（まだ開始されていない場合）
                    if (!concretePresenter.IsActive)
                    {
                        concretePresenter.Start();
                    }
                    
                    Debug.Log("MVPHUDManager: プレイヤーヘルスプロバイダー設定完了");
                }
                else
                {
                    Debug.LogError("MVPHUDManager: ヘルスプレゼンターの型が期待されるものと異なります");
                }
            }
            else
            {
                Debug.LogError("MVPHUDManager: ヘルスHUDプレゼンターが見つかりません");
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
            _pendingActions.Clear();
            
            Debug.Log("MVPHUDManager: HUDリソース解放完了");
        }
        
        /// <summary>
        /// デバッグ用: 全体の状態を診断
        /// </summary>
        [ContextMenu("Diagnose MVP HUD System")]
        public void DiagnoseMVPSystem()
        {
            Debug.Log("MVPHUDManager: === MVP HUDシステム診断 ===");
            Debug.Log($"MVPHUDManager: 初期化状態: {_isInitialized}");
            Debug.Log($"MVPHUDManager: 表示状態: {_isVisible}");
            Debug.Log($"MVPHUDManager: プレゼンター数: {_presenters.Count}");
            Debug.Log($"MVPHUDManager: HUDキャンバス: {(_hudCanvas != null ? _hudCanvas.name : "null")}");
            Debug.Log($"MVPHUDManager: ヘルスHUDビュー: {(_healthHUDView != null ? _healthHUDView.name : "null")}");
            Debug.Log($"MVPHUDManager: ラウンドカウントHUDビュー: {(_roundCountHUDView != null ? _roundCountHUDView.name : "null")}");
            Debug.Log($"MVPHUDManager: ゲームタイマーHUDビュー: {(_gameTimerHUDView != null ? _gameTimerHUDView.name : "null")}");
            
            // 各Viewのテスト
            if (_healthHUDView != null)
            {
                _healthHUDView.TestSliderUpdate(0.5f);
            }
            
            if (_roundCountHUDView != null)
            {
                _roundCountHUDView.TestUpdateAll();
            }
            
            if (_gameTimerHUDView != null)
            {
                _gameTimerHUDView.TestTimerDisplay();
            }
            
            // Presenterの状態確認
            foreach (var kvp in _presenters)
            {
                Debug.Log($"MVPHUDManager: プレゼンター: {kvp.Key.Name} -> {kvp.Value.GetType().Name}, アクティブ: {kvp.Value.IsActive}");
            }
            
            Debug.Log("MVPHUDManager: === 診断完了 ===");
        }
    }
}