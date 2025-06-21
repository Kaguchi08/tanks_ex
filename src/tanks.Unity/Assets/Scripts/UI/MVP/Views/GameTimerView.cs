using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace Complete.UI.MVP
{
    /// <summary>
    /// ゲーム時間HUD用のViewクラス
    /// ラウンド経過時間の表示UIを担当
    /// </summary>
    public class GameTimerView : MonoBehaviour, IGameTimerView
    {
        [Header("UI Components")]
        [SerializeField] private Text _roundTimeText;
        
        [Header("Display Settings")]
        [SerializeField] private string _roundTimeFormat = "{0:mm\\:ss}";
        
        [Header("Visual Settings")]
        [SerializeField] private Color _runningColor = Color.white;
        [SerializeField] private Color _pausedColor = Color.gray;
        
        private bool _isActive = true;
        private bool _isRunning = false;
        
        public bool IsActive => _isActive;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            // 自動検出を試行
            if (_roundTimeText == null)
            {
                _roundTimeText = transform.Find("RoundTimeText")?.GetComponent<Text>();
            }
            
            // 見つからない場合は警告
            if (_roundTimeText == null)
                Debug.LogWarning("GameTimerView: ラウンド時間テキストが見つかりません", this);
                
            Debug.Log($"GameTimerView: ゲームタイマービュー初期化完了 - Round: {_roundTimeText?.name}");
        }
        
        public async UniTask InitializeAsync()
        {
            await UniTask.DelayFrame(1);
            
            // 初期状態の設定
            UpdateRoundTime(0f);
            SetRunningState(false);
        }
        
        public void Show()
        {
            if (!_isActive)
            {
                _isActive = true;
                gameObject.SetActive(true);
                Debug.Log("GameTimerView: ゲームタイマービューを表示");
            }
        }
        
        public void Hide()
        {
            if (_isActive)
            {
                _isActive = false;
                gameObject.SetActive(false);
                Debug.Log("GameTimerView: ゲームタイマービューを非表示");
            }
        }
        
        public void UpdateRoundTime(float timeInSeconds)
        {
            if (_roundTimeText != null)
            {
                var timeSpan = System.TimeSpan.FromSeconds(timeInSeconds);
                _roundTimeText.text = string.Format(_roundTimeFormat, timeSpan);
                Debug.Log($"GameTimerView: ラウンド時間を {timeInSeconds:F1}s に更新");
            }
        }
        
        
        public void SetRunningState(bool isRunning)
        {
            _isRunning = isRunning;
            Color targetColor = isRunning ? _runningColor : _pausedColor;
            
            if (_roundTimeText != null)
            {
                _roundTimeText.color = targetColor;
            }
            
            Debug.Log($"GameTimerView: タイマー実行状態を {isRunning} に設定");
        }
        
        /// <summary>
        /// 時間をフォーマットして表示用文字列に変換
        /// </summary>
        /// <param name="timeInSeconds">時間（秒）</param>
        /// <returns>フォーマットされた時間文字列</returns>
        public string FormatTime(float timeInSeconds)
        {
            var timeSpan = System.TimeSpan.FromSeconds(timeInSeconds);
            
            if (timeSpan.TotalHours >= 1)
            {
                return string.Format("{0:h\\:mm\\:ss}", timeSpan);
            }
            else
            {
                return string.Format("{0:mm\\:ss}", timeSpan);
            }
        }
        
        /// <summary>
        /// テスト用: 時間表示をテスト
        /// </summary>
        [ContextMenu("Test Timer Display")]
        public void TestTimerDisplay()
        {
            UpdateRoundTime(125.5f); // 2:05
            SetRunningState(true);
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        public void Dispose()
        {
            // 必要に応じてクリーンアップを追加
        }
    }
}