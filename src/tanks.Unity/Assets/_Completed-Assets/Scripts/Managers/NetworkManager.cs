using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using Tanks.Shared;
using UnityEngine;

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
        [SerializeField] private GameObject _networkTankPrefab;

        // MagicOnionのクライアント
        private ITankGameHub _client;
        private GrpcChannelx _channel;
        private int _myPlayerId = -1;
        private Dictionary<int, GameObject> _remoteTanks = new Dictionary<int, GameObject>();
        
        // リモートタンクのTransformキャッシュ
        private Dictionary<int, Transform> _remoteTankTransforms = new Dictionary<int, Transform>();
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
            DisconnectAsync().Forget();
            _cts?.Cancel();
            _cts?.Dispose();
        }

        /// <summary>
        /// サーバーに接続する
        /// </summary>
        public async UniTaskVoid ConnectAsync()
        {
            try
            {
                Debug.Log($"サーバーに接続を試みています: {_serverAddress}:{_serverPort}");
                
                // チャンネルを作成
                _channel = GrpcChannelx.ForAddress($"http://{_serverAddress}:{_serverPort}");
                
                // StreamingHubクライアントを作成
                _client = await StreamingHubClient.ConnectAsync<ITankGameHub, ITankGameHubReceiver>(_channel, this);
                
                // サーバーに参加
                string playerName = "Player_" + UnityEngine.Random.Range(1, 9999);
                await _client.JoinAsync(playerName);
                
                Debug.Log("サーバーに接続しました");
                
                // 位置情報の定期送信を開始
                StartSendingPositionAsync().Forget();
            }
            catch (Exception ex)
            {
                Debug.LogError($"接続エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// サーバーから切断する
        /// </summary>
        public async UniTaskVoid DisconnectAsync()
        {
            if (_client != null)
            {
                try
                {
                    await _client.LeaveAsync();
                    await _client.DisposeAsync();
                    await _channel.ShutdownAsync();
                    
                    _client = null;
                    _channel = null;
                    Debug.Log("サーバーから切断しました");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"切断エラー: {ex.Message}");
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
                    if (_myPlayerId > 0 && _gameManager.m_Tanks.Length >= _myPlayerId)
                    {
                        var tank = _gameManager.m_Tanks[_myPlayerId - 1];
                        if (tank != null && tank.m_Instance != null)
                        {
                            var position = tank.m_Instance.transform.position;
                            var rotation = tank.m_Instance.transform.rotation;

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
        private void SpawnRemoteTank(int playerId)
        {
            if (_remoteTanks.ContainsKey(playerId))
                return;

            // リモートタンクのスポーン位置
            Vector3 spawnPosition = new Vector3(0, 0, 0);
            Quaternion spawnRotation = Quaternion.identity;

            // プレイヤーIDに基づいてスポーン位置を調整
            if (playerId == 1)
            {
                spawnPosition = new Vector3(-10, 0, 0);
            }
            else
            {
                spawnPosition = new Vector3(10, 0, 0);
            }

            // リモートタンクを生成
            GameObject remoteTank = Instantiate(_networkTankPrefab, spawnPosition, spawnRotation);
            remoteTank.name = $"RemoteTank_{playerId}";
            
            // リモートタンクの移動と発射コンポーネントを無効化（サーバーからの位置情報で動かす）
            var movement = remoteTank.GetComponent<TankMovement>();
            var shooting = remoteTank.GetComponent<TankShooting>();
            
            if (movement != null) movement.enabled = false;
            if (shooting != null) shooting.enabled = false;

            // リモートタンクを管理対象に追加
            _remoteTanks[playerId] = remoteTank;
            _remoteTankTransforms[playerId] = remoteTank.transform;

            Debug.Log($"リモートタンクを生成しました: PlayerId={playerId}");
        }

        /// <summary>
        /// 発射処理
        /// </summary>
        public async void FireAsync()
        {
            if (_client != null && _myPlayerId > 0)
            {
                try
                {
                    await _client.FireAsync(_myPlayerId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"発射エラー: {ex.Message}");
                }
            }
        }

        #region ITankGameHubReceiver の実装
        public void OnJoin(int playerID, string playerName)
        {
            Debug.Log($"プレイヤーが参加しました: ID={playerID}, Name={playerName}");
            
            // 自分のプレイヤーIDを設定
            if (_myPlayerId < 0)
            {
                _myPlayerId = playerID;
                Debug.Log($"自分のプレイヤーID: {_myPlayerId}");
            }
            else if (playerID != _myPlayerId)
            {
                // 他のプレイヤーのタンクを生成
                SpawnRemoteTank(playerID);
            }
        }

        public void OnLeave(int playerID)
        {
            Debug.Log($"プレイヤーが退出しました: ID={playerID}");
            
            // リモートタンクを削除
            if (_remoteTanks.TryGetValue(playerID, out GameObject remoteTank))
            {
                Destroy(remoteTank);
                _remoteTanks.Remove(playerID);
                _remoteTankTransforms.Remove(playerID);
            }
        }

        public void OnGameStart()
        {
            Debug.Log("ゲームが開始されました");
        }

        public void OnUpdatePosition(TankPositionData positionData)
        {
            // 自分のタンクの位置情報は無視
            if (positionData.PlayerID == _myPlayerId)
                return;

            // リモートタンクの位置情報を更新
            if (_remoteTankTransforms.TryGetValue(positionData.PlayerID, out Transform transform))
            {
                // 位置を更新
                transform.position = new Vector3(
                    positionData.PositionX,
                    positionData.PositionY,
                    positionData.PositionZ
                );
                
                // 回転を更新
                transform.rotation = Quaternion.Euler(0, positionData.RotationY, 0);
            }
            else
            {
                // リモートタンクがまだ生成されていない場合は生成
                SpawnRemoteTank(positionData.PlayerID);
            }
        }

        public void OnFire(int playerID)
        {
            // 自分の発射は無視
            if (playerID == _myPlayerId)
                return;

            // リモートタンクの発射処理
            if (_remoteTanks.TryGetValue(playerID, out GameObject remoteTank))
            {
                var shooting = remoteTank.GetComponent<TankShooting>();
                if (shooting != null)
                {
                    shooting.Fire();
                }
            }
        }
        #endregion
    }
} 