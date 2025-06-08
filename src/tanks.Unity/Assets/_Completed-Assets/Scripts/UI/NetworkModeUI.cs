using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

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
                .Subscribe(_ => { OnConnectButtonClicked(); UpdateUI(); })
                .AddTo(this);
            _disconnectButton.OnClickAsObservable()
                .Subscribe(_ => { OnDisconnectButtonClicked(); UpdateUI(); })
                .AddTo(this);
        }

        private void Update()
        {
            // 接続状態を更新
            if (_statusText != null)
            {
                if (_networkManager.IsConnected)
                {
                    _statusText.text = "接続状態: 接続済み";
                    _statusText.color = Color.green;
                }
                else if (_networkManager.IsNetworkMode)
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
        }

        private void UpdateUI()
        {
            // ボタンの有効/無効状態を更新
            _connectButton.interactable = _networkManager.IsNetworkMode && !_networkManager.IsConnected;
            _disconnectButton.interactable = _networkManager.IsConnected;
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

        private void OnConnectButtonClicked()
        {
            // サーバーに接続
            _networkManager.ConnectAsync().Forget();
            UpdateUI();
        }

        private void OnDisconnectButtonClicked()
        {
            // サーバーから切断
            _networkManager.DisconnectAsync().Forget();
            UpdateUI();
        }
    }
} 