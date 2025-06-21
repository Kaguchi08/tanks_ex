using System;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Complete.UI.MVP
{
    /// <summary>
    /// ラウンド数HUD用のPresenterクラス
    /// ModelとViewの仲介を担当し、ラウンド数関連のビジネスロジックを処理
    /// </summary>
    public class RoundCountPresenter : IRoundCountPresenter
    {
        private IRoundCountModel _model;
        private IRoundCountView _view;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private bool _isActive = false;
        
        public bool IsActive => _isActive;
        
        public async UniTask InitializeAsync()
        {
            if (_view != null)
            {
                await _view.InitializeAsync();
            }
            
            Debug.Log("RoundCountPresenter: ラウンドカウントプレゼンター初期化完了");
        }
        
        public void SetRoundCountModel(IRoundCountModel roundCountModel)
        {
            if (_model != null && _isActive)
            {
                // 既存のModelから購読を解除
                UnsubscribeFromModel();
            }
            
            _model = roundCountModel;
            
            if (_model != null && _isActive)
            {
                // 新しいModelに購読
                SubscribeToModel();
                // 現在の状態を即座に反映
                UpdateViewFromModel();
            }
            
            Debug.Log($"RoundCountPresenter: モデル設定 - {roundCountModel?.GetType().Name}");
        }
        
        public void SetView(IRoundCountView view)
        {
            _view = view;
            Debug.Log($"RoundCountPresenter: ビュー設定 - {view?.GetType().Name}");
        }
        
        public IRoundCountModel GetModel()
        {
            return _model;
        }
        
        public void Start()
        {
            if (_isActive)
            {
                Debug.LogWarning("RoundCountPresenter: ラウンドカウントプレゼンターは既にアクティブです");
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
            
            Debug.Log("RoundCountPresenter: ラウンドカウントプレゼンター開始");
        }
        
        public void Stop()
        {
            if (!_isActive)
            {
                Debug.LogWarning("RoundCountPresenter: ラウンドカウントプレゼンターは既に非アクティブです");
                return;
            }
            
            _isActive = false;
            UnsubscribeFromModel();
            
            if (_view != null)
            {
                _view.Hide();
            }
            
            Debug.Log("RoundCountPresenter: ラウンドカウントプレゼンター停止");
        }
        
        private void SubscribeToModel()
        {
            if (_model == null) return;
            
            // ラウンド数変更の監視
            _model.OnRoundChanged
                .Subscribe(round => UpdateCurrentRound(round))
                .AddTo(_disposables);
            
            // Player1勝利数変更の監視
            _model.OnPlayer1WinsChanged
                .Subscribe(wins => UpdatePlayer1Wins(wins))
                .AddTo(_disposables);
            
            // Player2勝利数変更の監視
            _model.OnPlayer2WinsChanged
                .Subscribe(wins => UpdatePlayer2Wins(wins))
                .AddTo(_disposables);
            
            // モデル変更全般の監視
            _model.OnChanged
                .Subscribe(_ => UpdateViewFromModel())
                .AddTo(_disposables);
                
            Debug.Log("RoundCountPresenter: モデルイベント購読開始");
        }
        
        private void UnsubscribeFromModel()
        {
            _disposables.Clear();
            Debug.Log("RoundCountPresenter: モデルイベント購読終了");
        }
        
        private void UpdateViewFromModel()
        {
            if (_model == null || _view == null) return;
            
            UpdateCurrentRound(_model.CurrentRound);
            UpdatePlayer1Wins(_model.Player1Wins);
            UpdatePlayer2Wins(_model.Player2Wins);
        }
        
        private void UpdateCurrentRound(int currentRound)
        {
            if (_view == null) return;
            
            _view.UpdateCurrentRound(currentRound);
            Debug.Log($"RoundCountPresenter: 現在ラウンドを{currentRound}に更新");
        }
        
        private void UpdatePlayer1Wins(int wins)
        {
            if (_view == null) return;
            
            _view.UpdatePlayer1Wins(wins);
            Debug.Log($"RoundCountPresenter: Player1勝利数を{wins}に更新");
        }
        
        private void UpdatePlayer2Wins(int wins)
        {
            if (_view == null) return;
            
            _view.UpdatePlayer2Wins(wins);
            Debug.Log($"RoundCountPresenter: Player2勝利数を{wins}に更新");
        }
        
        public void Dispose()
        {
            Stop();
            _disposables?.Dispose();
            _model?.Dispose();
            _view?.Dispose();
            
            Debug.Log("RoundCountPresenter: リソース解放完了");
        }
    }
}