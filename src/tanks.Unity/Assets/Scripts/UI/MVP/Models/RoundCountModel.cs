using System;
using UniRx;
using UnityEngine;

namespace Complete.UI.MVP
{
    /// <summary>
    /// ラウンド数HUD用のModelクラス
    /// ゲームの状態からラウンド数と勝利数の情報を管理
    /// </summary>
    public class RoundCountModel : IRoundCountModel
    {
        private int _currentRound = 0;
        private int _roundsToWin = 5;
        private int _player1Wins = 0;
        private int _player2Wins = 0;
        
        private readonly Subject<Unit> _onChangedSubject = new Subject<Unit>();
        private readonly Subject<int> _onRoundChangedSubject = new Subject<int>();
        private readonly Subject<int> _onPlayer1WinsChangedSubject = new Subject<int>();
        private readonly Subject<int> _onPlayer2WinsChangedSubject = new Subject<int>();
        
        // IModel implementation
        public IObservable<Unit> OnChanged => _onChangedSubject.AsObservable();
        
        // IRoundCountModel implementation
        public int CurrentRound => _currentRound;
        public int RoundsToWin => _roundsToWin;
        public int Player1Wins => _player1Wins;
        public int Player2Wins => _player2Wins;
        
        public IObservable<int> OnRoundChanged => _onRoundChangedSubject.AsObservable();
        public IObservable<int> OnPlayer1WinsChanged => _onPlayer1WinsChangedSubject.AsObservable();
        public IObservable<int> OnPlayer2WinsChanged => _onPlayer2WinsChangedSubject.AsObservable();
        
        /// <summary>
        /// 勝利に必要なラウンド数を設定
        /// </summary>
        public void SetRoundsToWin(int roundsToWin)
        {
            if (_roundsToWin != roundsToWin)
            {
                _roundsToWin = roundsToWin;
                _onChangedSubject.OnNext(Unit.Default);
                Debug.Log($"RoundCountModel: 勝利必要ラウンド数を {_roundsToWin} に設定");
            }
        }
        
        /// <summary>
        /// 現在のラウンド数を更新
        /// </summary>
        public void UpdateCurrentRound(int currentRound)
        {
            if (_currentRound != currentRound)
            {
                _currentRound = currentRound;
                _onRoundChangedSubject.OnNext(_currentRound);
                _onChangedSubject.OnNext(Unit.Default);
                Debug.Log($"RoundCountModel: 現在ラウンド数を {_currentRound} に更新");
            }
        }
        
        /// <summary>
        /// Player1の勝利数を更新
        /// </summary>
        public void UpdatePlayer1Wins(int wins)
        {
            if (_player1Wins != wins)
            {
                _player1Wins = wins;
                _onPlayer1WinsChangedSubject.OnNext(_player1Wins);
                _onChangedSubject.OnNext(Unit.Default);
                Debug.Log($"RoundCountModel: Player1勝利数を {_player1Wins} に更新");
            }
        }
        
        /// <summary>
        /// Player2の勝利数を更新
        /// </summary>
        public void UpdatePlayer2Wins(int wins)
        {
            if (_player2Wins != wins)
            {
                _player2Wins = wins;
                _onPlayer2WinsChangedSubject.OnNext(_player2Wins);
                _onChangedSubject.OnNext(Unit.Default);
                Debug.Log($"RoundCountModel: Player2勝利数を {_player2Wins} に更新");
            }
        }
        
        /// <summary>
        /// GameManagerから状態を同期
        /// </summary>
        public void SyncFromGameManager(GameManager gameManager)
        {
            if (gameManager == null) return;
            
            SetRoundsToWin(gameManager.m_NumRoundsToWin);
            
            // GameManagerからのラウンド数は内部的に管理されているため
            // 直接取得できない場合があります。必要に応じて公開メソッドを追加してください。
        }
        
        /// <summary>
        /// TankManagerの配列から勝利数を同期
        /// </summary>
        public void SyncWinsFromTankManagers(TankManager[] tankManagers)
        {
            if (tankManagers == null) return;
            
            int player1Wins = 0;
            int player2Wins = 0;
            
            foreach (var tank in tankManagers)
            {
                if (tank?.m_Instance != null)
                {
                    switch (tank.m_PlayerID)
                    {
                        case 1:
                            player1Wins = tank.m_Wins;
                            break;
                        case 2:
                            player2Wins = tank.m_Wins;
                            break;
                    }
                }
            }
            
            UpdatePlayer1Wins(player1Wins);
            UpdatePlayer2Wins(player2Wins);
        }
        
        public void Dispose()
        {
            _onChangedSubject?.Dispose();
            _onRoundChangedSubject?.Dispose();
            _onPlayer1WinsChangedSubject?.Dispose();
            _onPlayer2WinsChangedSubject?.Dispose();
        }
    }
}