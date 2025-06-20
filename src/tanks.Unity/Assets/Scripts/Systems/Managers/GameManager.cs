using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;
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
        public bool m_UseNetworkMode = false;

        [Header("References")]
        public CameraControl m_CameraControl;
        public Text m_MessageText;
        public GameObject m_TankPrefab;
        public TankManager[] m_Tanks;     // 既存のTankManagerを使用（ITankControllerインターフェース対応済み）
        public NetworkManager m_NetworkManager;
        public Complete.UI.MVP.MVPHUDManager m_MVPHUDManager;  // MVPベースのHUDManager

        
        private ITankController[] _tankControllers;
        private int _roundNumber;
        private bool _gameStarted = false;
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        private void Start()
        {
            // ネットワークモードの設定を反映
            if (m_NetworkManager != null)
            {
                // NetworkManagerのネットワークモード設定を反映
                m_UseNetworkMode = m_NetworkManager.IsNetworkMode;
            }

            if (m_UseNetworkMode)
            {
                // ネットワークモードでは、接続が完了してから初期化
                Debug.Log("ネットワークモード: 接続待機中...");
                // NetworkManagerからの通知を待つ
            }
            else
            {
                // ローカルモードでは即座に初期化
                InitializeGame();
            }
        }

        /// <summary>
        /// ゲームの初期化（タンク生成など）
        /// </summary>
        public void InitializeGame()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("ゲームは既に初期化されています");
                return;
            }

            Debug.Log($"ゲームを初期化します - NetworkMode: {m_UseNetworkMode}");

            // SpawnPointの設定を検証
            ValidateSpawnPoints();

            // タンクを生成してセットアップ
            SpawnAllTanks();
            SetCameraTargets();

            // タンクコントローラー配列を準備（生成されたタンクのみ）
            var activeControllers = new System.Collections.Generic.List<ITankController>();
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance != null)
                {
                    activeControllers.Add(m_Tanks[i]);
                }
            }
            _tankControllers = activeControllers.ToArray();

            _isInitialized = true;
            Debug.Log($"ゲーム初期化完了 - アクティブタンク数: {_tankControllers.Length}");

            // MVPHUDManagerを初期化時にセットアップ（非同期で実行）
            if (m_MVPHUDManager != null)
            {
                _ = InitializeHUDWithDelay();
            }

            if (!m_UseNetworkMode)
            {
                // ローカルモードでは即座にゲーム開始
                StartGame();
            }
        }

        /// <summary>
        /// SpawnPointの設定を検証し、問題があれば修正する
        /// </summary>
        private void ValidateSpawnPoints()
        {
            Debug.Log($"=== SpawnPoint検証開始 ===");
            Debug.Log($"TankManager配列サイズ: {m_Tanks.Length}");

            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i] == null)
                {
                    Debug.LogError($"TankManager[{i}]がnullです");
                    continue;
                }

                if (m_Tanks[i].m_SpawnPoint == null)
                {
                    Debug.LogError($"TankManager[{i}].m_SpawnPointがnullです");
                    continue;
                }

                Vector3 pos = m_Tanks[i].m_SpawnPoint.position;
                Debug.Log($"TankManager[{i}] - Type: {m_Tanks[i].m_TankType}, SpawnPoint: {pos}");
            }

            // SpawnPointの重複をチェック
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                for (int j = i + 1; j < m_Tanks.Length; j++)
                {
                    if (m_Tanks[i].m_SpawnPoint != null && m_Tanks[j].m_SpawnPoint != null)
                    {
                        Vector3 pos1 = m_Tanks[i].m_SpawnPoint.position;
                        Vector3 pos2 = m_Tanks[j].m_SpawnPoint.position;
                        
                        float distance = Vector3.Distance(pos1, pos2);
                        if (distance < 1f) // 1メートル以内は重複とみなす
                        {
                            Debug.LogWarning($"SpawnPointが重複しています: TankManager[{i}]={pos1}, TankManager[{j}]={pos2}");
                            
                            // 自動修正: 2番目のSpawnPointを横に5メートルずらす
                            Vector3 newPos = pos2 + Vector3.right * 5f;
                            m_Tanks[j].m_SpawnPoint.position = newPos;
                            Debug.Log($"TankManager[{j}]のSpawnPointを自動修正しました: {newPos}");
                        }
                    }
                }
            }

            Debug.Log($"=== SpawnPoint検証終了 ===");
        }

        /// <summary>
        /// ゲームを開始する（ネットワークからの通知または即座に）
        /// </summary>
        public void StartGame()
        {
            if (_gameStarted)
            {
                Debug.LogWarning("ゲームは既に開始されています");
                return;
            }

            if (!_isInitialized)
            {
                Debug.LogError("ゲームが初期化されていません。先にInitializeGame()を呼び出してください。");
                return;
            }

            _gameStarted = true;
            Debug.Log("ゲームループを開始します");
            
            // タイマーを開始
            if (m_MVPHUDManager != null)
            {
                m_MVPHUDManager.SetGameTimerRunning(true);
            }
            
            // ゲームループを非同期で開始
            _ = GameLoopAsync(); // fire and forget
        }

        private void SpawnAllTanks()
        {
            if (m_UseNetworkMode)
            {
                // ネットワークモードでは、最初のPlayerタンクのみを生成
                SpawnLocalPlayerTank();
                Debug.Log("ネットワークモード: ローカルプレイヤータンクを生成しました");
            }
            else
            {
                // ローカルモードでは、全てのタンクを生成
                SpawnAllLocalTanks();
                Debug.Log("ローカルモード: 全てのタンクを生成しました");
            }
        }

        private void SpawnAllLocalTanks()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                SpawnTank(i);
            }
        }

        private void SpawnLocalPlayerTank()
        {
            // NetworkManagerから自分のPlayerIDを取得して適切なスロットに配置
            if (m_UseNetworkMode && m_NetworkManager != null)
            {
                // ネットワークモードでは後でPlayerIDが決まってから生成
                Debug.Log("ネットワークモード: PlayerID確定後にローカルタンクを生成します");
                return;
            }

            // ローカルモード用: 最初のPlayerタイプのタンクを生成
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_TankType == TankType.Player)
                {
                    SpawnTank(i);
                    Debug.Log($"ローカルプレイヤータンクを生成しました: Slot {i}");
                    break;
                }
            }
        }

        /// <summary>
        /// 特定のPlayerIDに対応するローカルタンクを生成
        /// </summary>
        public void SpawnLocalPlayerTankWithID(int playerID)
        {
            Debug.Log($"=== ローカルタンク生成（PlayerID指定）開始 ===");
            Debug.Log($"PlayerID: {playerID}");

            // PlayerIDの範囲チェック（2人対戦用）
            if (playerID < 1 || playerID > 2)
            {
                Debug.LogError($"PlayerID {playerID}は2人対戦の範囲外です（1-2が有効）。フォールバック処理を実行します。");
                SpawnLocalPlayerTankFallback(playerID);
                return;
            }

            // PlayerIDに基づいてスロットを決定（PlayerID 1 → Slot 0, PlayerID 2 → Slot 1）
            int targetSlot = playerID - 1;
            Debug.Log($"計算されたターゲットスロット: {targetSlot}");
            
            // スロットの検証
            if (targetSlot >= 0 && targetSlot < m_Tanks.Length)
            {
                Debug.Log($"Slot {targetSlot}の状態:");
                Debug.Log($"  TankType: {m_Tanks[targetSlot].m_TankType}");
                Debug.Log($"  Instance: {(m_Tanks[targetSlot].m_Instance != null ? "存在" : "null")}");
                if (m_Tanks[targetSlot].m_SpawnPoint != null)
                {
                    Debug.Log($"  SpawnPoint: {m_Tanks[targetSlot].m_SpawnPoint.position}");
                }
            }
            
            if (targetSlot >= 0 && targetSlot < m_Tanks.Length && 
                m_Tanks[targetSlot].m_TankType == TankType.Player &&
                m_Tanks[targetSlot].m_Instance == null)
            {
                Debug.Log($"ターゲットスロット {targetSlot} を使用してタンクを生成します");
                SpawnTankWithPlayerID(targetSlot, playerID);
                Debug.Log($"PlayerID {playerID}のローカルタンクをSlot {targetSlot}に生成しました");
            }
            else
            {
                Debug.LogError($"ターゲットスロット {targetSlot} は使用できません。フォールバック処理を実行します。");
                SpawnLocalPlayerTankFallback(playerID);
            }
            
            Debug.Log($"=== ローカルタンク生成（PlayerID指定）終了 ===");
        }

        /// <summary>
        /// フォールバック: 空いているスロットを探してローカルタンクを生成
        /// </summary>
        private void SpawnLocalPlayerTankFallback(int playerID)
        {
            Debug.Log("フォールバック検索を開始します...");
            bool found = false;
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                Debug.Log($"Slot {i}検査中: Type={m_Tanks[i].m_TankType}, Instance={(m_Tanks[i].m_Instance != null ? "存在" : "null")}");
                if (m_Tanks[i].m_TankType == TankType.Player && m_Tanks[i].m_Instance == null)
                {
                    Debug.Log($"フォールバック: Slot {i} を使用してタンクを生成します");
                    SpawnTankWithPlayerID(i, playerID);
                    Debug.Log($"フォールバック: PlayerID {playerID}のローカルタンクをSlot {i}に生成しました");
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                Debug.LogError("利用可能なPlayerスロットが見つかりませんでした");
            }
        }

        private void SpawnTank(int index)
        {
            int playerNumber = index + 1;
            SpawnTankWithPlayerID(index, playerNumber);
        }

        private void SpawnTankWithPlayerID(int index, int playerID)
        {
            Debug.Log($"=== タンク生成開始 ===");
            Debug.Log($"Slot: {index}, PlayerID: {playerID}");
            Debug.Log($"SpawnPoint位置: {m_Tanks[index].m_SpawnPoint.position}");
            Debug.Log($"TankType: {m_Tanks[index].m_TankType}");
            
            // タンクのインスタンスを作成
            GameObject tankInstance = Instantiate(m_TankPrefab, m_Tanks[index].m_SpawnPoint.position, m_Tanks[index].m_SpawnPoint.rotation);
            Debug.Log($"タンクインスタンス生成完了: {tankInstance.name} at {tankInstance.transform.position}");
            
            // タンクの種類に応じたInputProviderと名前を設定
            IInputProvider inputProvider;
            string playerName;

            if (m_Tanks[index].m_TankType == TankType.AI)
            {
                inputProvider = new AIInputProvider();
                playerName = "AI " + playerID;
            }
            else
            {
                // ネットワークモードでは全てのローカルプレイヤーがPlayer1の入力を使用
                // ローカルモードでは従来通りスロットベースの入力番号を使用
                int inputNumber = m_UseNetworkMode ? 1 : (index + 1);
                inputProvider = new LocalInputProvider(inputNumber);
                playerName = "PLAYER " + playerID;
            }

            // タンクをセットアップ（PlayerIDを使用）
            m_Tanks[index].Setup(tankInstance, inputProvider, playerID, playerName);
            Debug.Log($"タンクセットアップ完了: {playerName}, InputProvider: {inputProvider.GetType().Name}");
            
            // プレイヤーのタンクの場合、HUDManagerにHP情報を設定
            if (m_Tanks[index].m_TankType == TankType.Player && IsMyPlayerTank(index))
            {
                SetupPlayerHUD(index);
            }
            
            // ネットワークモードの場合は、TankShootingが自動的にネットワーク通知を行う
            // そのためここでは特別な処理は不要
            
            Debug.Log($"=== タンク生成完了: Slot={index}, PlayerID={playerID}, Position={tankInstance.transform.position} ===");
        }

        /// <summary>
        /// ネットワークからリモートプレイヤーのタンクを動的に追加
        /// </summary>
        public void AddRemotePlayerTank(int playerID, string playerName)
        {
            Debug.Log($"リモートプレイヤーのタンクを追加開始: PlayerID={playerID}, Name={playerName}");
            
            // PlayerIDの範囲チェック（2人対戦用）
            if (playerID < 1 || playerID > 2)
            {
                Debug.LogError($"PlayerID {playerID}は2人対戦の範囲外です（1-2が有効）。フォールバック処理を実行します。");
                AddRemotePlayerTankFallback(playerID, playerName);
                return;
            }
            
            // PlayerIDに基づいてスロットを決定（PlayerID 1 → Slot 0, PlayerID 2 → Slot 1）
            int targetSlot = playerID - 1;
            
            if (targetSlot >= 0 && targetSlot < m_Tanks.Length && 
                m_Tanks[targetSlot].m_TankType == TankType.Player &&
                m_Tanks[targetSlot].m_Instance == null)
            {
                // 指定されたスロットが空いている場合
                SpawnRemoteTank(targetSlot, playerID, playerName);
                Debug.Log($"PlayerID {playerID}のリモートタンクをSlot {targetSlot}に生成しました");
                return;
            }
            
            // 指定スロットが使用済みの場合、フォールバック処理
            Debug.Log($"PlayerID {playerID}の推奨スロット{targetSlot}は使用済み、フォールバック処理を実行します");
            AddRemotePlayerTankFallback(playerID, playerName);
        }

        /// <summary>
        /// フォールバック: 空いているスロットを探してリモートタンクを生成
        /// </summary>
        private void AddRemotePlayerTankFallback(int playerID, string playerName)
        {
            Debug.Log("リモートタンクのフォールバック検索を開始します...");
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_TankType == TankType.Player && m_Tanks[i].m_Instance == null)
                {
                    SpawnRemoteTank(i, playerID, playerName);
                    Debug.Log($"PlayerID {playerID}のリモートタンクを代替Slot {i}に生成しました");
                    return;
                }
            }
            
            Debug.LogWarning($"空いているPlayerタンクスロットが見つかりません: PlayerID={playerID}");
        }

        /// <summary>
        /// 指定されたスロットにリモートタンクを生成
        /// </summary>
        private void SpawnRemoteTank(int slotIndex, int playerID, string playerName)
        {
            Debug.Log($"=== リモートタンク生成開始 ===");
            Debug.Log($"SlotIndex: {slotIndex}, PlayerID: {playerID}, Name: {playerName}");
            
            // そのTankManagerのスポーン地点を使用
            Vector3 spawnPosition = m_Tanks[slotIndex].m_SpawnPoint.position;
            Quaternion spawnRotation = m_Tanks[slotIndex].m_SpawnPoint.rotation;
            Debug.Log($"使用SpawnPoint: {spawnPosition}");
            
            // タンクのインスタンスを作成
            GameObject tankInstance = Instantiate(m_TankPrefab, spawnPosition, spawnRotation);
            Debug.Log($"リモートタンクインスタンス生成: {tankInstance.name} at {tankInstance.transform.position}");
            
            // RemoteInputProviderを使用（リモートプレイヤー用）
            var inputProvider = new RemoteInputProvider();
            Debug.Log($"RemoteInputProvider作成完了");
            
            // タンクをセットアップ
            m_Tanks[slotIndex].Setup(tankInstance, inputProvider, playerID, playerName);
            Debug.Log($"リモートタンクセットアップ完了: {playerName}");
            
            // カメラターゲットを更新
            SetCameraTargets();
            
            // タンクコントローラー配列を更新
            UpdateTankControllers();
            
            Debug.Log($"=== リモートタンク生成完了: PlayerID={playerID}, Slot={slotIndex}, Position={tankInstance.transform.position} ===");
        }

        /// <summary>
        /// リモートプレイヤーのInputProviderを取得
        /// </summary>
        public RemoteInputProvider GetRemoteInputProvider(int playerID)
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance != null && 
                    m_Tanks[i].m_PlayerID == playerID &&
                    m_Tanks[i].InputProvider is RemoteInputProvider remoteProvider)
                {
                    return remoteProvider;
                }
            }
            return null;
        }

        /// <summary>
        /// PlayerIDからTankManagerを取得
        /// </summary>
        public TankManager GetTankManager(int playerID)
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance != null && m_Tanks[i].m_PlayerID == playerID)
                {
                    return m_Tanks[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 現在のゲーム状態を取得
        /// </summary>
        public (int currentRound, bool gameStarted, bool isInitialized) GetCurrentGameState()
        {
            return (_roundNumber, _gameStarted, _isInitialized);
        }
        
        /// <summary>
        /// 現在のラウンド数を取得
        /// </summary>
        public int GetCurrentRound()
        {
            return _roundNumber;
        }
        
        /// <summary>
        /// ゲーム開始済みかどうか
        /// </summary>
        public bool IsGameStarted()
        {
            return _gameStarted;
        }
        
        /// <summary>
        /// HUD初期化を少し遅延させて実行
        /// </summary>
        private async UniTask InitializeHUDWithDelay()
        {
            // フレームを待ってからHUDを初期化
            await UniTask.DelayFrame(2);
            
            if (m_MVPHUDManager != null && m_MVPHUDManager.IsInitialized)
            {
                m_MVPHUDManager.SyncGameStateToRoundCountHUD(this);
                Debug.Log("GameManager: HUD初期化遅延完了");
            }
            else
            {
                // HUDが初期化されていない場合、さらに待機
                await UniTask.WaitUntil(() => m_MVPHUDManager != null && m_MVPHUDManager.IsInitialized, 
                                      cancellationToken: this.GetCancellationTokenOnDestroy());
                m_MVPHUDManager.SyncGameStateToRoundCountHUD(this);
                Debug.Log("GameManager: HUD初期化待機後完了");
            }
        }
        
        /// <summary>
        /// 後から接続したクライアント用：ゲーム状態を同期
        /// </summary>
        public void SyncGameStateForLateJoiner()
        {
            if (!_isInitialized || m_MVPHUDManager == null) return;
            
            // 非同期で同期を実行
            _ = SyncGameStateForLateJoinerAsync();
        }
        
        /// <summary>
        /// 後から接続したクライアント用：ゲーム状態を同期（非同期版）
        /// </summary>
        private async UniTask SyncGameStateForLateJoinerAsync()
        {
            Debug.Log($"Late joiner用ゲーム状態同期開始: Round={_roundNumber}, GameStarted={_gameStarted}");
            
            // HUDが初期化されるまで待機
            if (m_MVPHUDManager != null && !m_MVPHUDManager.IsInitialized)
            {
                Debug.Log("Late joiner: HUD初期化を待機中...");
                await UniTask.WaitUntil(() => m_MVPHUDManager.IsInitialized, 
                                      cancellationToken: this.GetCancellationTokenOnDestroy());
                Debug.Log("Late joiner: HUD初期化完了を確認");
            }
            
            if (m_MVPHUDManager == null) return;
            
            // ゲーム状態をHUDに同期
            m_MVPHUDManager.SyncGameStateToRoundCountHUD(this);
            
            // ゲームが開始済みの場合
            if (_gameStarted && _roundNumber > 0)
            {
                // 進行中のラウンドの場合、時間も含めて同期
                // 注意: ネットワーク対戦では正確な時間同期のためサーバーからの情報が必要
                // ここでは暫定的に新しいラウンドとして開始
                m_MVPHUDManager.StartNewRound(_roundNumber);
                m_MVPHUDManager.SetGameTimerRunning(true);
                
                Debug.Log($"Late joiner: ラウンド{_roundNumber}として同期しタイマー開始");
            }
            else if (_gameStarted)
            {
                // ゲーム開始済みだがラウンド0の場合
                m_MVPHUDManager.SetGameTimerRunning(true);
                Debug.Log("Late joiner: ゲーム開始済み、タイマーのみ開始");
            }
            
            Debug.Log("Late joiner用ゲーム状態同期完了");
        }
        
        /// <summary>
        /// 外部から特定のラウンド時間で同期（サーバーからの正確な情報用）
        /// </summary>
        public void SyncGameStateWithTime(int roundNumber, float roundTime, bool isGameStarted)
        {
            if (!_isInitialized || m_MVPHUDManager == null) return;
            
            Debug.Log($"正確な時間での同期: Round={roundNumber}, Time={roundTime:F1}s, Started={isGameStarted}");
            
            _roundNumber = roundNumber;
            _gameStarted = isGameStarted;
            
            // ゲーム状態をHUDに同期
            m_MVPHUDManager.SyncGameStateToRoundCountHUD(this);
            
            if (isGameStarted && roundNumber > 0)
            {
                // 正確な時間でラウンドを同期
                m_MVPHUDManager.SyncRoundTime(roundNumber, roundTime);
            }
            else if (isGameStarted)
            {
                m_MVPHUDManager.SetGameTimerRunning(true);
            }
            
            Debug.Log("正確な時間での同期完了");
        }
        
        /// <summary>
        /// 自分のPlayerタンクのPlayerIDを取得
        /// </summary>
        public int GetMyPlayerID()
        {
            // ネットワークモードではNetworkManagerから取得
            if (m_UseNetworkMode && m_NetworkManager != null)
            {
                int networkPlayerId = m_NetworkManager.MyPlayerId;
                Debug.Log($"GetMyPlayerID (Network): {networkPlayerId}");
                return networkPlayerId;
            }
            
            // ローカルモードでは従来通り
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_TankType == TankType.Player && 
                    m_Tanks[i].m_Instance != null &&
                    m_Tanks[i].InputProvider is LocalInputProvider)
                {
                    Debug.Log($"GetMyPlayerID (Local): {m_Tanks[i].m_PlayerID}");
                    return m_Tanks[i].m_PlayerID;
                }
            }
            
            Debug.LogWarning("GetMyPlayerID: No matching player found, returning -1");
            return -1;
        }


        private void SetCameraTargets()
        {
            var activeTargets = new System.Collections.Generic.List<Transform>();

            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance != null)
                {
                    activeTargets.Add(m_Tanks[i].m_Instance.transform);
                }
            }

            m_CameraControl.m_Targets = activeTargets.ToArray();
            Debug.Log($"カメラターゲットを更新しました: {activeTargets.Count}台のタンク");
        }

        /// <summary>
        /// タンクコントローラー配列を更新
        /// </summary>
        private void UpdateTankControllers()
        {
            if (!_isInitialized) return;

            var activeControllers = new System.Collections.Generic.List<ITankController>();
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance != null)
                {
                    activeControllers.Add(m_Tanks[i]);
                }
            }
            _tankControllers = activeControllers.ToArray();
            Debug.Log($"タンクコントローラーを更新しました: {_tankControllers.Length}台のタンク");
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

                // HUDに新しいラウンドを通知
                if (m_MVPHUDManager != null)
                {
                    m_MVPHUDManager.StartNewRound(_roundNumber);
                }

                // ラウンド開始状態
                var roundStartingState = new RoundStartingState(m_StartDelay, m_MessageText, m_CameraControl, _tankControllers, _roundNumber);
                await ExecuteStateAsync(roundStartingState, token);

                // ラウンドプレイ状態
                var roundPlayingState = new RoundPlayingState(m_MessageText, _tankControllers);
                await ExecuteStateAsync(roundPlayingState, token);

                // ラウンド終了状態
                var roundEndingState = new RoundEndingState(m_EndDelay, m_MessageText, _tankControllers, m_NumRoundsToWin, _roundNumber);
                await ExecuteStateAsync(roundEndingState, token);

                // HUDに勝利数を同期
                if (m_MVPHUDManager != null)
                {
                    m_MVPHUDManager.SyncGameStateToRoundCountHUD(this);
                }

                // ゲーム勝者がいる場合、シーンを再読み込み
                if (roundEndingState.GameWinner != null)
                {
                    // タイマーを停止
                    if (m_MVPHUDManager != null)
                    {
                        m_MVPHUDManager.SetGameTimerRunning(false);
                    }
                    
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

        private bool IsMyPlayerTank(int tankIndex)
        {
            if (m_UseNetworkMode && m_NetworkManager != null)
            {
                int myPlayerID = GetMyPlayerID();
                int tankPlayerID = m_Tanks[tankIndex].m_PlayerID;
                bool isMyTank = tankPlayerID == myPlayerID;
                Debug.Log($"IsMyPlayerTank (Network): Tank[{tankIndex}] PlayerID={tankPlayerID}, MyPlayerID={myPlayerID}, IsMyTank={isMyTank}");
                return isMyTank;
            }
            else
            {
                bool isPlayerType = m_Tanks[tankIndex].m_TankType == TankType.Player;
                Debug.Log($"IsMyPlayerTank (Local): Tank[{tankIndex}] TankType={m_Tanks[tankIndex].m_TankType}, IsPlayerType={isPlayerType}");
                return isPlayerType;
            }
        }

        private void SetupPlayerHUD(int tankIndex)
        {
            Debug.Log($"=== SetupPlayerHUD called for tank {tankIndex} ===");
            Debug.Log($"MVP HUD Manager: {(m_MVPHUDManager != null ? m_MVPHUDManager.name : "null")}");
            
            if (m_MVPHUDManager != null)
            {
                TankHealth tankHealth = m_Tanks[tankIndex].GetTankHealth();
                if (tankHealth != null)
                {
                    Debug.Log($"TankHealth found: {tankHealth.GetType().Name}");
                    Debug.Log($"Current Health: {tankHealth.CurrentHealth}");
                    Debug.Log($"Max Health: {tankHealth.MaxHealth}");
                    
                    // MVPHUDManagerの初期化完了を待ってからHUD設定を実行
                    _ = SetupPlayerHUDAsync(tankIndex, tankHealth);
                }
                else
                {
                    Debug.LogError($"TankHealth component not found on tank {tankIndex}");
                }
            }
            else
            {
                Debug.LogError("MVPHUDManagerが設定されていません");
            }
        }
        
        private async UniTask SetupPlayerHUDAsync(int tankIndex, TankHealth tankHealth)
        {
            try
            {
                Debug.Log($"SetupPlayerHUDAsync: Waiting for MVPHUDManager initialization...");
                
                // MVPHUDManagerの初期化完了を待機
                await m_MVPHUDManager.WaitForInitializationAsync();
                
                Debug.Log($"SetupPlayerHUDAsync: MVPHUDManager initialized, setting health provider");
                
                // HUD設定を実行
                m_MVPHUDManager.SetPlayerHealthProvider(tankHealth);
                m_MVPHUDManager.ShowAll();
                
                Debug.Log($"MVPHUDManager: プレイヤータンク（Slot:{tankIndex}）のHPを設定しました");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SetupPlayerHUDAsync failed for tank {tankIndex}: {ex}");
            }
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