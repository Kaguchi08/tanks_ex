using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Complete.Interfaces;
using Cysharp.Threading.Tasks;

namespace Complete.GameStates
{
    /// <summary>
    /// ラウンド開始状態を管理するクラス
    /// 単一責任の原則に従ってラウンド開始処理のみを担当
    /// </summary>
    public class RoundStartingState : IGameState
    {
        private readonly float _startDelay;
        private readonly Text _messageText;
        private readonly CameraControl _cameraControl;
        private readonly ITankController[] _tankControllers;
        private readonly int _roundNumber;

        public string StateName => "Round Starting";

        public RoundStartingState(float startDelay, Text messageText, CameraControl cameraControl, ITankController[] tankControllers, int roundNumber)
        {
            _startDelay = startDelay;
            _messageText = messageText;
            _cameraControl = cameraControl;
            _tankControllers = tankControllers;
            _roundNumber = roundNumber;
        }

        public async UniTask EnterAsync(System.Threading.CancellationToken token)
        {
            // タンクをリセットし、制御を無効にする
            ResetAllTanks();
            DisableTankControl();

            // カメラの位置とサイズを設定
            _cameraControl.SetStartPositionAndSize();

            // ラウンド番号を表示
            _messageText.text = "ROUND " + _roundNumber;

            // 指定時間待機
            await UniTask.Delay(System.TimeSpan.FromSeconds(_startDelay), cancellationToken: token);
        }

        public void Exit()
        {
            // 状態終了時の処理（必要に応じて）
        }

        private void ResetAllTanks()
        {
            foreach (var tankController in _tankControllers)
            {
                tankController.Reset();
            }
        }

        private void DisableTankControl()
        {
            foreach (var tankController in _tankControllers)
            {
                tankController.Disable();
            }
        }
    }
} 