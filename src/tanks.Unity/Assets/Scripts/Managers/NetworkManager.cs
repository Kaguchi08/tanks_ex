using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using Tanks.Shared;
using UnityEngine;
using Complete.Input;
using Complete.Utility;

namespace Complete
{
    /// <summary>
    /// ネットワーク通信を管理するマネージャークラス
    /// </summary>
    public class NetworkManager : MonoBehaviour, ITankGameHubReceiver
    {
        [Header("Network Settings")]
        [SerializeField] private string _serverAddress = "localhost";
        [SerializeField] private int _serverPort = 5044;
        [SerializeField] private bool _useNetworkMode = false;

        [Header("Game References")]
        [SerializeField] private GameManager _gameManager;

        // MagicOnionのクライアント
        private ITankGameHub _client;
        private GrpcChannelx _channel;
        private int _myPlayerId = -1;
        private Dictionary<int, RemoteInputProvider> _remoteInputProviders = new Dictionary<int, RemoteInputProvider>();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public bool IsNetworkMode => _useNetworkMode;
        public bool IsConnected => _client != null;

        private void Start()
        {
            if (_useNetworkMode)
            {
                // ネットワークモードが有効な場合、サーバーに接続
                ConnectAsync().Forget();
            }
        }

        private void OnDestroy()
        {
            try
            {
                _cts?.Cancel();
                DisconnectAsync().Forget();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"OnDestroy中のエラー: {ex.Message}");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// サーバーに接続する
        /// </summary>
        public async UniTask ConnectAsync()
        {
            try
            {
                Debug.Log($"サーバーに接続を試みています: {_serverAddress}:{_serverPort}");
                
                // チャンネルを作成
                _channel = GrpcChannelx.ForAddress($"http://{_serverAddress}:{_serverPort}");
                
                // StreamingHubクライアントを作成
                _client = await StreamingHubClient.ConnectAsync<ITankGameHub, ITankGameHubReceiver>(_channel, this, cancellationToken: _cts.Token);
                
                // サーバーに参加
                string playerName = "Player_" + UnityEngine.Random.Range(1, 9999);
                await _client.JoinAsync(playerName);
                
                Debug.Log("サーバーに接続しました");
                
                // 接続が完了したらGameManagerを初期化
                if (_gameManager != null)
                {
                    Debug.Log("ネットワーク接続完了 - GameManagerを初期化します");
                    _gameManager.InitializeGame();
                }
                else
                {
                    Debug.LogError("GameManagerが見つかりません");
                }
                
                // 位置情報の定期送信を開始
                StartSendingPositionAsync().Forget();
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("接続がキャンセルされました");
            }
            catch (Exception ex)
            {
                Debug.LogError($"接続エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// サーバーから切断する
        /// </summary>
        public async UniTask DisconnectAsync()
        {
            if (_client != null)
            {
                try
                {
                    await _client.LeaveAsync();
                    await _client.DisposeAsync();
                    
                    if (_channel != null)
                    {
                        await _channel.ShutdownAsync();
                    }
                    
                    Debug.Log("サーバーから切断しました");
                }
                catch (System.OperationCanceledException)
                {
                    Debug.Log("切断処理がキャンセルされました");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"切断エラー: {ex.Message}");
                }
                finally
                {
                    _client = null;
                    _channel = null;
                }
            }
        }

        /// <summary>
        /// 位置情報を定期的に送信する
        /// </summary>
        private async UniTaskVoid StartSendingPositionAsync()
        {
            while (_client != null && !_cts.IsCancellationRequested)
            {
                try
                {
                    // 自分のタンクの位置情報を取得
                    var myTank = GameUtility.GetTankManagerByPlayerId(_gameManager.m_Tanks, _myPlayerId);
                    if (myTank != null && myTank.m_Instance != null)
                    {
                        var position = myTank.m_Instance.transform.position;
                        var rotation = myTank.m_Instance.transform.rotation;

                        // 位置情報を送信
                        await _client.UpdatePositionAsync(new TankPositionData
                        {
                            PlayerID = _myPlayerId,
                            PositionX = position.x,
                            PositionY = position.y,
                            PositionZ = position.z,
                            RotationY = rotation.eulerAngles.y
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"位置情報送信エラー: {ex.Message}");
                }

                // 100ミリ秒待機
                await UniTask.Delay(100, cancellationToken: _cts.Token);
            }
        }

        /// <summary>
        /// リモートプレイヤーのタンクを生成
        /// </summary>
        private void SpawnRemoteTank(int playerId, string playerName)
        {
            if (_remoteInputProviders.ContainsKey(playerId))
            {
                Debug.Log($"PlayerId={playerId}のリモートタンクは既に存在します");
                return;
            }

            Debug.Log($"リモートプレイヤーのタンクを生成開始: PlayerId={playerId}, Name={playerName}");

            // GameManagerにリモートプレイヤーのタンクを追加
            _gameManager.AddRemotePlayerTank(playerId, playerName);

            // RemoteInputProviderを取得して管理
            var remoteInputProvider = _gameManager.GetRemoteInputProvider(playerId);
            if (remoteInputProvider != null)
            {
                _remoteInputProviders[playerId] = remoteInputProvider;
                Debug.Log($"RemoteInputProviderを登録しました: PlayerId={playerId}");
            }
            else
            {
                Debug.LogWarning($"RemoteInputProviderが見つかりません: PlayerId={playerId}");
            }

            Debug.Log($"リモートプレイヤーのタンクを生成完了: PlayerId={playerId}, Name={playerName}");
        }

        /// <summary>
        /// 発射処理
        /// </summary>
        public async UniTaskVoid FireAsync(ShellFireData fireData)
        {
            if (_client != null && _myPlayerId > 0)
            {
                try
                {
                    await _client.FireAsync(fireData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"発射エラー: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// ヘルス更新処理
        /// </summary>
        public async UniTaskVoid UpdateHealthAsync(int playerID, float currentHealth)
        {
            if (_client != null)
            {
                try
                {
                    await _client.UpdateHealthAsync(playerID, currentHealth);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ヘルス更新エラー: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 爆発力適用処理
        /// </summary>
        public async UniTaskVoid ApplyExplosionForceAsync(ExplosionForceData explosionData)
        {
            if (_client != null)
            {
                try
                {
                    await _client.ApplyExplosionForceAsync(explosionData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"爆発力適用エラー: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// ゲーム結果通知処理
        /// </summary>
        public async UniTaskVoid NotifyGameResultAsync(GameResultData gameResult)
        {
            if (_client != null)
            {
                try
                {
                    await _client.NotifyGameResultAsync(gameResult);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ゲーム結果通知エラー: {ex.Message}");
                }
            }
        }


        #region ITankGameHubReceiver の実装
        public void OnJoin(int playerID, string playerName)
        {
            Debug.Log($"プレイヤーが参加しました: ID={playerID}, Name={playerName}");
            
            // PlayerIDを2人対戦用に正規化（サーバー側が修正されていない場合の保護）
            int normalizedPlayerID = NormalizePlayerIDForNetworking(playerID);
            if (normalizedPlayerID != playerID)
            {
                Debug.LogWarning($"PlayerID {playerID}を{normalizedPlayerID}に正規化しました");
            }
            
            // 自分のプレイヤーIDを設定
            if (_myPlayerId < 0)
            {
                _myPlayerId = normalizedPlayerID;
                Debug.Log($"自分のプレイヤーID: {_myPlayerId}");
                
                // 自分のローカルタンクをPlayerIDベースで生成
                if (_gameManager != null)
                {
                    _gameManager.SpawnLocalPlayerTankWithID(normalizedPlayerID);
                    Debug.Log($"PlayerID {normalizedPlayerID}で自分のローカルタンクを生成しました");
                }
            }
            else if (normalizedPlayerID != _myPlayerId)
            {
                // 他のプレイヤーのタンクを生成
                SpawnRemoteTank(normalizedPlayerID, playerName);
            }
            else
            {
                // 自分の情報の重複受信（既存プレイヤー情報の同期）
                Debug.Log($"自分の情報を再受信: ID={normalizedPlayerID}");
            }
        }

        /// <summary>
        /// PlayerIDを2人対戦用に正規化（1-2の範囲）
        /// ネットワーク通信で使用する特別な正規化ロジック
        /// </summary>
        private int NormalizePlayerIDForNetworking(int playerID)
        {
            // 既に正しい範囲の場合はそのまま返す
            if (playerID >= 1 && playerID <= 2)
            {
                return playerID;
            }
            
            // 自分のプレイヤーIDが未設定の場合は1を割り当て
            if (_myPlayerId < 0)
            {
                return 1;
            }
            
            // 既に自分のPlayerIDが1の場合は2を割り当て、そうでなければ1を割り当て
            return _myPlayerId == 1 ? 2 : 1;
        }

        public void OnLeave(int playerID)
        {
            Debug.Log($"プレイヤーが退出しました: ID={playerID}");
            
            // RemoteInputProviderを削除
            if (_remoteInputProviders.TryGetValue(playerID, out RemoteInputProvider provider))
            {
                provider.Dispose();
                _remoteInputProviders.Remove(playerID);
            }
            
            // GameManagerのタンクも削除が必要（別途実装予定）
            // TODO: GameManagerにRemoveRemoteTankメソッドを追加
        }

        public void OnGameStart()
        {
            Debug.Log("サーバーからゲーム開始通知を受信しました");
            
            // GameManagerにゲーム開始を通知
            if (_gameManager != null)
            {
                // GameManagerが初期化されていない場合は初期化
                if (!_gameManager.IsInitialized)
                {
                    Debug.Log("GameManagerが未初期化のため、先に初期化します");
                    _gameManager.InitializeGame();
                }
                
                Debug.Log("ゲームを開始します");
                _gameManager.StartGame();
            }
            else
            {
                Debug.LogError("GameManagerが見つかりません");
            }
        }

        public void OnUpdatePosition(TankPositionData positionData)
        {
            // PlayerIDを正規化
            int normalizedPlayerID = NormalizePlayerIDForNetworking(positionData.PlayerID);
            
            // 自分のタンクの位置情報は無視
            if (normalizedPlayerID == _myPlayerId)
                return;

            // RemoteInputProviderに入力データを設定（位置情報から入力を逆算）
            // 注意: この実装は暫定的で、実際にはサーバーから入力情報を直接受信すべき
            if (_remoteInputProviders.TryGetValue(normalizedPlayerID, out RemoteInputProvider provider))
            {
                // TODO: 位置情報から移動・回転入力を逆算して設定する
                // 現在は位置の直接更新を行う（暫定的な実装）
                
                // GameManagerからリモートタンクのTransformを取得して直接更新
                var tankManager = GameUtility.GetTankManagerByPlayerId(_gameManager.m_Tanks, normalizedPlayerID);
                if (tankManager?.m_Instance != null)
                {
                    var movement = tankManager.GetTankMovement();
                    
                    // 物理演算オーバーライド中は位置同期をスキップ
                    if (movement == null || !movement.IsPhysicsOverride)
                    {
                        var transform = tankManager.m_Instance.transform;
                        transform.position = new Vector3(
                            positionData.PositionX,
                            positionData.PositionY,
                            positionData.PositionZ
                        );
                        transform.rotation = Quaternion.Euler(0, positionData.RotationY, 0);
                    }
                }
            }
            else
            {
                // リモートタンクがまだ生成されていない場合は生成
                SpawnRemoteTank(normalizedPlayerID, $"Remote_{normalizedPlayerID}");
            }
        }

        public void OnFire(ShellFireData fireData)
        {
            // PlayerIDを正規化
            int normalizedPlayerID = NormalizePlayerIDForNetworking(fireData.PlayerID);
            
            // 自分の発射は無視
            if (normalizedPlayerID == _myPlayerId)
                return;

            // リモートプレイヤーのタンクで砲弾を発射
            var tankManager = GameUtility.GetTankManagerByPlayerId(_gameManager.m_Tanks, normalizedPlayerID);
            if (tankManager != null)
            {
                var shooting = tankManager.GetTankShooting();
                if (shooting != null)
                {
                    shooting.FireFromNetwork(fireData);
                }
            }
        }

        public void OnHealthUpdate(int playerID, float currentHealth)
        {
            // PlayerIDを正規化
            int normalizedPlayerID = NormalizePlayerIDForNetworking(playerID);
            
            // 自分のヘルス更新は無視
            if (normalizedPlayerID == _myPlayerId)
                return;

            // リモートプレイヤーのヘルスを更新
            var tankManager = GameUtility.GetTankManagerByPlayerId(_gameManager.m_Tanks, normalizedPlayerID);
            if (tankManager != null)
            {
                var health = tankManager.GetTankHealth();
                if (health != null)
                {
                    health.SetHealthFromNetwork(currentHealth);
                }
            }
        }

        public void OnExplosionForce(ExplosionForceData explosionData)
        {
            // PlayerIDを正規化
            int normalizedPlayerID = NormalizePlayerIDForNetworking(explosionData.TargetPlayerID);

            // 対象タンクに爆発力を適用
            var tankManager = GameUtility.GetTankManagerByPlayerId(_gameManager.m_Tanks, normalizedPlayerID);
            if (tankManager?.m_Instance != null)
            {
                var rigidbody = tankManager.m_Instance.GetComponent<Rigidbody>();
                var movement = tankManager.GetTankMovement();
                
                if (rigidbody != null && movement != null)
                {
                    Vector3 explosionPosition = new Vector3(explosionData.ExplosionX, explosionData.ExplosionY, explosionData.ExplosionZ);
                    
                    // 物理演算オーバーライドを設定（位置同期を一時停止）
                    movement.SetPhysicsOverride(2f);
                    
                    // 爆発力を適用
                    rigidbody.AddExplosionForce(explosionData.Force, explosionPosition, explosionData.Radius);

                    // ダメージも適用（ネットワーク経由なので同期なし）
                    var health = tankManager.GetTankHealth();
                    if (health != null)
                    {
                        health.TakeDamageFromNetwork(explosionData.Damage);
                    }
                }
            }
        }

        public void OnGameResult(GameResultData gameResult)
        {
            Debug.Log($"ゲーム結果を受信: 勝者={gameResult.WinnerName} (ID:{gameResult.WinnerPlayerID}), ラウンド={gameResult.RoundNumber}, ゲーム終了={gameResult.IsGameEnd}");
            
            // ゲーム結果をUIに反映する処理を追加可能
            // 現在は単純にログ出力のみ
        }


        #endregion
    }
} 