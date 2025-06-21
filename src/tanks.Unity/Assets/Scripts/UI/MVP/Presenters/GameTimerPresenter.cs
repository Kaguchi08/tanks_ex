using System;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Complete.UI.MVP
{
    /// <summary>
    /// ゲーム時間HUD用のPresenterクラス
    /// ModelとViewの仲介を担当し、時間関連のビジネスロジックを処理
    /// </summary>
    public class GameTimerPresenter : IGameTimerPresenter
    {
        private IGameTimerModel _model;
        private IGameTimerView _view;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private bool _isActive = false;
        
        public bool IsActive => _isActive;
        
        public async UniTask InitializeAsync()
        {
            if (_view != null)
            {
                await _view.InitializeAsync();
            }
            
            Debug.Log("GameTimerPresenter: ゲームタイマープレゼンター初期化完了");
        }
        
        public void SetGameTimerModel(IGameTimerModel gameTimerModel)
        {
            if (_model != null && _isActive)
            {
                // 既存のModelから購読を解除
                UnsubscribeFromModel();
            }
            
            _model = gameTimerModel;
            
            if (_model != null && _isActive)
            {
                // 新しいModelに購読
                SubscribeToModel();
                // 現在の状態を即座に反映
                UpdateViewFromModel();
            }
            
            Debug.Log($"GameTimerPresenter: モデル設定 - {gameTimerModel?.GetType().Name}");
        }
        
        public void SetView(IGameTimerView view)
        {
            _view = view;
            Debug.Log($"GameTimerPresenter: ビュー設定 - {view?.GetType().Name}");
        }
        
        public IGameTimerModel GetModel()
        {
            return _model;
        }
        
        public void Start()
        {
            if (_isActive)
            {
                Debug.LogWarning("GameTimerPresenter: ゲームタイマープレゼンターは既にアクティブです");
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
            
            Debug.Log("GameTimerPresenter: ゲームタイマープレゼンター開始");
        }
        
        public void Stop()
        {
            if (!_isActive)
            {
                Debug.LogWarning("GameTimerPresenter: ゲームタイマープレゼンターは既に非アクティブです");
                return;
            }
            
            _isActive = false;
            UnsubscribeFromModel();
            
            if (_view != null)
            {
                _view.Hide();
            }
            
            Debug.Log("GameTimerPresenter: ゲームタイマープレゼンター停止");
        }
        
        private void SubscribeToModel()
        {
            if (_model == null) return;
            
            // ラウンド時間変更の監視
            _model.OnRoundTimeChanged
                .Subscribe(time => UpdateRoundTime(time))
                .AddTo(_disposables);
            
            // タイマー動作状態変更の監視
            _model.OnRunningStateChanged
                .Subscribe(isRunning => UpdateRunningState(isRunning))
                .AddTo(_disposables);
            
            // モデル変更全般の監視
            _model.OnChanged
                .Subscribe(_ => UpdateViewFromModel())
                .AddTo(_disposables);
                
            Debug.Log("GameTimerPresenter: モデルイベント購読開始");
        }
        
        private void UnsubscribeFromModel()
        {
            _disposables.Clear();
            Debug.Log("GameTimerPresenter: モデルイベント購読終了");
        }
        
        private void UpdateViewFromModel()
        {
            if (_model == null || _view == null) return;
            
            UpdateRoundTime(_model.CurrentRoundTime);
            UpdateRunningState(_model.IsRunning);
        }
        
        private void UpdateRoundTime(float timeInSeconds)
        {
            if (_view == null) return;
            
            _view.UpdateRoundTime(timeInSeconds);
            Debug.Log($"GameTimerPresenter: ラウンド時間更新 {timeInSeconds:F1}秒");
        }
        
        
        private void UpdateRunningState(bool isRunning)
        {
            if (_view == null) return;
            
            _view.SetRunningState(isRunning);
            Debug.Log($"GameTimerPresenter: 動作状態更新 {isRunning}");
        }
        
        public void Dispose()
        {
            Stop();
            _disposables?.Dispose();
            _model?.Dispose();
            _view?.Dispose();
            
            Debug.Log("GameTimerPresenter: ゲームタイマープレゼンターリソース解放");
        }
    }
}