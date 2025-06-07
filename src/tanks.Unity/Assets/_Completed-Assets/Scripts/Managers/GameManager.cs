using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Complete.Interfaces;
using Complete.GameStates;
using Complete.Input;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Complete
{
    /// <summary>
    /// リファクタリングされたGameManager
    /// SOLID原則に従って設計された状態管理システムを使用
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public int m_NumRoundsToWin = 5;
        public float m_StartDelay = 3f;
        public float m_EndDelay = 3f;

        [Header("References")]
        public CameraControl m_CameraControl;
        public Text m_MessageText;
        public GameObject m_TankPrefab;
        public TankManager[] m_Tanks;     // 既存のTankManagerを使用（ITankControllerインターフェース対応済み）

        
        private ITankController[] _tankControllers;
        private int _roundNumber;

        private void Start()
        {
            // タンクを生成してセットアップ
            SpawnAllTanks();
            SetCameraTargets();

            // タンクコントローラー配列を準備
            _tankControllers = new ITankController[m_Tanks.Length];
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                _tankControllers[i] = m_Tanks[i];
            }

            // ゲームループを非同期で開始
            _ = GameLoopAsync(); // fire and forget
        }


        private void SpawnAllTanks()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                // タンクのインスタンスを作成
                GameObject tankInstance = Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation);
                
                // タンクの種類に応じたInputProviderと名前を設定
                IInputProvider inputProvider;
                string playerName;
                int playerNumber = i + 1;

                if (m_Tanks[i].m_TankType == TankType.AI)
                {
                    inputProvider = new AIInputProvider();
                    playerName = "AI " + playerNumber;
                }
                else
                {
                    inputProvider = new LocalInputProvider(playerNumber);
                    playerName = "PLAYER " + playerNumber;
                }

                // タンクをセットアップ
                m_Tanks[i].Setup(tankInstance, inputProvider, playerNumber, playerName);
            }
        }


        private void SetCameraTargets()
        {
            Transform[] targets = new Transform[m_Tanks.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = m_Tanks[i].m_Instance.transform;
            }

            m_CameraControl.m_Targets = targets;
        }


        /// <summary>
        /// ゲームループ - 状態管理システムを使用
        /// </summary>
        private async UniTask GameLoopAsync()
        {
            CancellationToken token = this.GetCancellationTokenOnDestroy();

            while (!token.IsCancellationRequested)
            {
                _roundNumber++;

                // ラウンド開始状態
                var roundStartingState = new RoundStartingState(m_StartDelay, m_MessageText, m_CameraControl, _tankControllers, _roundNumber);
                await ExecuteStateAsync(roundStartingState, token);

                // ラウンドプレイ状態
                var roundPlayingState = new RoundPlayingState(m_MessageText, _tankControllers);
                await ExecuteStateAsync(roundPlayingState, token);

                // ラウンド終了状態
                var roundEndingState = new RoundEndingState(m_EndDelay, m_MessageText, _tankControllers, m_NumRoundsToWin);
                await ExecuteStateAsync(roundEndingState, token);

                // ゲーム勝者がいる場合、シーンを再読み込み
                if (roundEndingState.GameWinner != null)
                {
                    SceneManager.LoadScene(0);
                    return;
                }
            }
        }

        /// <summary>
        /// 状態を実行する
        /// </summary>
        private async UniTask ExecuteStateAsync(IGameState state, CancellationToken token)
        {
            await state.EnterAsync(token);
            state.Exit();
        }

        private void OnDestroy()
        {
            // InputProviderをクリーンアップ
            foreach (var tank in m_Tanks)
            {
                if (tank.InputProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}