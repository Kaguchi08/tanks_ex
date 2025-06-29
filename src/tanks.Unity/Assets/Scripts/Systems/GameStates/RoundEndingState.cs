using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Complete.Interfaces;
using Cysharp.Threading.Tasks;
using Tanks.Shared;
namespace Complete.GameStates
{
    /// <summary>
    /// ラウンド終了状態を管理するクラス
    /// 単一責任の原則に従ってラウンド終了処理のみを担当
    /// </summary>
    public class RoundEndingState : IGameState
    {
        private readonly float _endDelay;
        private readonly Text _messageText;
        private readonly ITankController[] _tankControllers;
        private readonly int _numRoundsToWin;
        private readonly CameraControl _cameraControl;
        private readonly NetworkManager _networkManager;
        private int _roundNumber;

        public string StateName => "Round Ending";

        public TankManager RoundWinner { get; private set; }
        public TankManager GameWinner { get; private set; }

        public RoundEndingState(float endDelay, Text messageText, ITankController[] tankControllers, int numRoundsToWin, int roundNumber = 0)
        {
            _endDelay = endDelay;
            _messageText = messageText;
            _tankControllers = tankControllers;
            _numRoundsToWin = numRoundsToWin;
            _roundNumber = roundNumber;
            
            // GameManagerから参照を取得
            _cameraControl = Object.FindObjectOfType<CameraControl>();
            _networkManager = Object.FindObjectOfType<NetworkManager>();
        }

        public async UniTask EnterAsync(System.Threading.CancellationToken token)
        {
            // タンクの制御を停止
            DisableTankControl();
            
            // 俯瞰視点カメラに戻す
            if (_cameraControl != null)
            {
                _cameraControl.ActivateTpsCamera(false);
            }

            // 前のラウンドの勝者をクリア
            RoundWinner = null;

            // ラウンドの勝者を確認
            RoundWinner = GetRoundWinner();

            // 勝者がいる場合、スコアを増加
            if (RoundWinner != null)
                RoundWinner.m_Wins++;

            // ゲームの勝者を確認
            GameWinner = GetGameWinner();

            // スコアとゲーム勝者に基づいてメッセージを取得し表示
            string message = CreateEndMessage();
            _messageText.text = message;

            // ネットワークモードでゲーム結果を同期
            if (_networkManager != null && _networkManager.IsNetworkMode && RoundWinner != null)
            {
                var gameResult = new GameResultData
                {
                    WinnerPlayerID = RoundWinner.m_PlayerID,
                    RoundNumber = _roundNumber,
                    IsGameEnd = GameWinner != null,
                    WinnerName = RoundWinner.m_PlayerName
                };
                
                _ = _networkManager.NotifyGameResultAsync(gameResult);
            }

            // 指定時間待機
            await UniTask.Delay(System.TimeSpan.FromSeconds(_endDelay), cancellationToken: token);
        }

        public void Exit()
        {
            // 状態終了時の処理（必要に応じて）
        }

        /// <summary>
        /// タンクの制御を無効にする
        /// </summary>
        private void DisableTankControl()
        {
            foreach (var tankController in _tankControllers)
            {
                tankController.Disable();
            }
        }

        /// <summary>
        /// ラウンドの勝者を取得
        /// </summary>
        /// <returns>勝者のTankManager、引き分けの場合はnull</returns>
        private TankManager GetRoundWinner()
        {
            foreach (var tankController in _tankControllers)
            {
                if (tankController is TankManager manager && 
                    manager.m_Instance != null && 
                    manager.m_Instance.activeSelf)
                {
                    return manager;
                }
            }

            // アクティブなタンクがない場合は引き分け
            return null;
        }

        /// <summary>
        /// ゲームの勝者を取得
        /// </summary>
        /// <returns>ゲーム勝者のTankManager、まだ勝者がいない場合はnull</returns>
        private TankManager GetGameWinner()
        {
            foreach (var tankController in _tankControllers)
            {
                if (tankController is TankManager manager && 
                    manager.m_Wins == _numRoundsToWin)
                {
                    return manager;
                }
            }

            return null;
        }

        /// <summary>
        /// 各ラウンド終了時に表示するメッセージを作成
        /// </summary>
        /// <returns>表示メッセージ</returns>
        private string CreateEndMessage()
        {
            // デフォルトは引き分けメッセージ
            string message = "DRAW!";

            // 勝者がいる場合はメッセージを変更
            if (RoundWinner != null)
                message = RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            // 改行を追加
            message += "\n\n\n\n";

            // 全タンクのスコアをメッセージに追加
            foreach (var tankController in _tankControllers)
            {
                if (tankController is TankManager manager)
                {
                    message += manager.m_ColoredPlayerText + ": " + manager.m_Wins + " WINS\n";
                }
            }

            // ゲーム勝者がいる場合、メッセージ全体を変更
            if (GameWinner != null)
                message = GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }
    }
} 