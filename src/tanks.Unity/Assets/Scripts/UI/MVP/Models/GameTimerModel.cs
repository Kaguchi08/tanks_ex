using System;
using UniRx;
using UnityEngine;

namespace Complete.UI.MVP
{
    /// <summary>
    /// ゲーム時間HUD用のModelクラス
    /// ゲームの経過時間を管理
    /// </summary>
    public class GameTimerModel : IGameTimerModel
    {
        private float _currentRoundTime = 0f;
        private float _totalGameTime = 0f;
        private bool _isRunning = false;
        private float _roundStartTime = 0f;
        private float _gameStartTime = 0f;
        
        private readonly Subject<Unit> _onChangedSubject = new Subject<Unit>();
        private readonly Subject<float> _onRoundTimeChangedSubject = new Subject<float>();
        private readonly Subject<float> _onTotalTimeChangedSubject = new Subject<float>();
        private readonly Subject<bool> _onRunningStateChangedSubject = new Subject<bool>();
        
        private IDisposable _updateTimerDisposable;
        
        // IModel implementation
        public IObservable<Unit> OnChanged => _onChangedSubject.AsObservable();
        
        // IGameTimerModel implementation
        public float CurrentRoundTime => _currentRoundTime;
        public float TotalGameTime => _totalGameTime;
        public bool IsRunning => _isRunning;
        
        public IObservable<float> OnRoundTimeChanged => _onRoundTimeChangedSubject.AsObservable();
        public IObservable<float> OnTotalTimeChanged => _onTotalTimeChangedSubject.AsObservable();
        public IObservable<bool> OnRunningStateChanged => _onRunningStateChangedSubject.AsObservable();
        
        /// <summary>
        /// タイマーを開始
        /// </summary>
        public void StartTimer()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            _gameStartTime = Time.time;
            _roundStartTime = Time.time;
            
            // 毎フレーム時間を更新
            _updateTimerDisposable = Observable.EveryUpdate()
                .Subscribe(_ => UpdateTime())
                .AddTo(new CompositeDisposable());
            
            _onRunningStateChangedSubject.OnNext(_isRunning);
            _onChangedSubject.OnNext(Unit.Default);
            
            Debug.Log("GameTimerModel: ゲームタイマー開始");
        }
        
        /// <summary>
        /// タイマーを停止
        /// </summary>
        public void StopTimer()
        {
            if (!_isRunning) return;
            
            _isRunning = false;
            _updateTimerDisposable?.Dispose();
            _updateTimerDisposable = null;
            
            _onRunningStateChangedSubject.OnNext(_isRunning);
            _onChangedSubject.OnNext(Unit.Default);
            
            Debug.Log("GameTimerModel: ゲームタイマー停止");
        }
        
        /// <summary>
        /// 新しいラウンドを開始
        /// </summary>
        public void StartNewRound()
        {
            _roundStartTime = Time.time;
            _currentRoundTime = 0f;
            
            _onRoundTimeChangedSubject.OnNext(_currentRoundTime);
            _onChangedSubject.OnNext(Unit.Default);
            
            Debug.Log("GameTimerModel: 新ラウンドタイマー開始");
        }
        
        /// <summary>
        /// ゲーム全体をリセット
        /// </summary>
        public void ResetGame()
        {
            StopTimer();
            _currentRoundTime = 0f;
            _totalGameTime = 0f;
            _gameStartTime = 0f;
            _roundStartTime = 0f;
            
            _onRoundTimeChangedSubject.OnNext(_currentRoundTime);
            _onTotalTimeChangedSubject.OnNext(_totalGameTime);
            _onChangedSubject.OnNext(Unit.Default);
            
            Debug.Log("GameTimerModel: ゲームタイマーリセット");
        }
        
        /// <summary>
        /// 時間を更新
        /// </summary>
        private void UpdateTime()
        {
            if (!_isRunning) return;
            
            float currentTime = Time.time;
            float newRoundTime = currentTime - _roundStartTime;
            float newTotalTime = currentTime - _gameStartTime;
            
            // ラウンド時間の更新
            if (Mathf.Abs(_currentRoundTime - newRoundTime) > 0.1f) // 0.1秒間隔で更新
            {
                _currentRoundTime = newRoundTime;
                _onRoundTimeChangedSubject.OnNext(_currentRoundTime);
            }
            
            // 総時間の更新
            if (Mathf.Abs(_totalGameTime - newTotalTime) > 0.1f) // 0.1秒間隔で更新
            {
                _totalGameTime = newTotalTime;
                _onTotalTimeChangedSubject.OnNext(_totalGameTime);
            }
            
            _onChangedSubject.OnNext(Unit.Default);
        }
        
        /// <summary>
        /// 手動で時間を設定（デバッグ用）
        /// </summary>
        public void SetRoundTime(float timeInSeconds)
        {
            _currentRoundTime = timeInSeconds;
            _onRoundTimeChangedSubject.OnNext(_currentRoundTime);
            _onChangedSubject.OnNext(Unit.Default);
        }
        
        /// <summary>
        /// 手動で総時間を設定（デバッグ用）
        /// </summary>
        public void SetTotalTime(float timeInSeconds)
        {
            _totalGameTime = timeInSeconds;
            _onTotalTimeChangedSubject.OnNext(_totalGameTime);
            _onChangedSubject.OnNext(Unit.Default);
        }
        
        /// <summary>
        /// 後から接続したクライアント用：進行中のラウンド時間を設定
        /// </summary>
        public void SyncRoundTime(float currentRoundTime, bool shouldStart = true)
        {
            _currentRoundTime = currentRoundTime;
            _roundStartTime = Time.time - currentRoundTime; // 逆算してスタート時間を設定
            
            _onRoundTimeChangedSubject.OnNext(_currentRoundTime);
            _onChangedSubject.OnNext(Unit.Default);
            
            if (shouldStart && !_isRunning)
            {
                StartTimer();
            }
            
            Debug.Log($"GameTimerModel: ラウンド時間同期完了: {currentRoundTime:F1}秒, 動作中: {_isRunning}");
        }
        
        public void Dispose()
        {
            StopTimer();
            _onChangedSubject?.Dispose();
            _onRoundTimeChangedSubject?.Dispose();
            _onTotalTimeChangedSubject?.Dispose();
            _onRunningStateChangedSubject?.Dispose();
        }
    }
}