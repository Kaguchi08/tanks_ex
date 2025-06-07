using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Complete.Interfaces;

namespace Complete.GameStates
{
    /// <summary>
    /// ラウンドプレイ中状態を管理するクラス
    /// 単一責任の原則に従ってラウンドプレイ処理のみを担当
    /// </summary>
    public class RoundPlayingState : IGameState
    {
        private readonly Text _messageText;
        private readonly ITankController[] _tankControllers;

        public string StateName => "Round Playing";

        public RoundPlayingState(Text messageText, ITankController[] tankControllers)
        {
            _messageText = messageText;
            _tankControllers = tankControllers;
        }

        public IEnumerator Enter()
        {
            // プレイヤーがタンクを制御できるようにする
            EnableTankControl();

            // 画面からテキストを消去
            _messageText.text = string.Empty;

            // 1台のタンクが残るまでループ
            while (!OneTankLeft())
            {
                yield return null;
            }
        }

        public void Exit()
        {
            // 状態終了時の処理（必要に応じて）
        }

        /// <summary>
        /// タンクの制御を有効にする
        /// </summary>
        private void EnableTankControl()
        {
            foreach (var tankController in _tankControllers)
            {
                tankController.Enable();
            }
        }

        /// <summary>
        /// 残りタンクが1台以下かチェック
        /// </summary>
        /// <returns>残りタンクが1台以下の場合true</returns>
        private bool OneTankLeft()
        {
            int numTanksLeft = 0;

            foreach (var tankController in _tankControllers)
            {
                if (tankController is TankManager manager && 
                    manager.m_Instance != null && 
                    manager.m_Instance.activeSelf)
                {
                    numTanksLeft++;
                }
            }

            return numTanksLeft <= 1;
        }
    }
} 