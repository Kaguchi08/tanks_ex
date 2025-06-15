using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;

namespace Complete
{
    /// <summary>
    /// ネットワークモードを切り替えるUIを制御するクラス
    /// </summary>
    public class NetworkModeUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Toggle _networkModeToggle;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _connectButton;
        [SerializeField] private Button _disconnectButton;

        [Header("References")]
        [SerializeField] private NetworkManager _networkManager;

        private void Start()
        {
            if (_networkManager == null)
            {
                Debug.LogError("NetworkManagerが設定されていません");
                return;
            }

            // トグルの初期状態を設定
            _networkModeToggle.isOn = _networkManager.IsNetworkMode;
            UpdateUI();

            // UniRxでイベントを購読
            _networkModeToggle.OnValueChangedAsObservable()
                .Subscribe(isOn => OnNetworkModeToggleChanged(isOn))
                .AddTo(this);
            _connectButton.OnClickAsObservable()
                .Subscribe(_ => OnConnectButtonClickedAsync())
                .AddTo(this);
            _disconnectButton.OnClickAsObservable()
                .Subscribe(_ => OnDisconnectButtonClickedAsync())
                .AddTo(this);

            // 接続状態の変更を監視してUIを更新
            Observable.EveryUpdate()
                .Select(_ => (_networkManager.IsConnected, _networkManager.IsNetworkMode))
                .DistinctUntilChanged()
                .Subscribe(state => UpdateConnectionStatus(state.IsConnected, state.IsNetworkMode))
                .AddTo(this);
        }

        private void UpdateConnectionStatus(bool isConnected, bool isNetworkMode)
        {
            if (_statusText != null)
            {
                if (isConnected)
                {
                    _statusText.text = "接続状態: 接続済み";
                    _statusText.color = Color.green;
                }
                else if (isNetworkMode)
                {
                    _statusText.text = "接続状態: 未接続";
                    _statusText.color = Color.yellow;
                }
                else
                {
                    _statusText.text = "接続状態: ローカルモード";
                    _statusText.color = Color.white;
                }
            }
            
            // ボタンの有効/無効状態を更新
            if (_connectButton != null)
                _connectButton.interactable = isNetworkMode && !isConnected;
            if (_disconnectButton != null)
                _disconnectButton.interactable = isConnected;
        }

        private void UpdateUI()
        {
            UpdateConnectionStatus(_networkManager.IsConnected, _networkManager.IsNetworkMode);
        }

        private void OnNetworkModeToggleChanged(bool isOn)
        {
            // トグル状態をNetworkManagerに反映
            var prop = typeof(NetworkManager).GetField("_useNetworkMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prop != null)
            {
                prop.SetValue(_networkManager, isOn);
            }

            // UIを更新
            UpdateUI();
        }

        private async UniTaskVoid OnConnectButtonClickedAsync()
        {
            try
            {
                await _networkManager.ConnectAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"接続エラー: {ex.Message}");
            }
        }

        private async UniTaskVoid OnDisconnectButtonClickedAsync()
        {
            try
            {
                await _networkManager.DisconnectAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"切断エラー: {ex.Message}");
            }
        }
    }
} 