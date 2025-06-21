using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace Complete.UI.MVP
{
    /// <summary>
    /// ラウンド数HUD用のViewクラス
    /// ラウンド数と勝利数の表示UIを担当
    /// </summary>
    public class RoundCountView : MonoBehaviour, IRoundCountView
    {
        [Header("UI Components")]
        [SerializeField] private Text _currentRoundText;
        [SerializeField] private Text _player1WinsText;
        [SerializeField] private Text _player2WinsText;
        
        [Header("Display Settings")]
        [SerializeField] private string _roundTextFormat = "Round {0}";
        [SerializeField] private string _winsTextFormat = "{0}";
        
        private bool _isActive = true;
        
        public bool IsActive => _isActive;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            // 自動検出を試行
            if (_currentRoundText == null)
            {
                _currentRoundText = transform.Find("CurrentRoundText")?.GetComponent<Text>();
            }
            
            if (_player1WinsText == null)
            {
                _player1WinsText = transform.Find("Player1WinsText")?.GetComponent<Text>();
            }
            
            if (_player2WinsText == null)
            {
                _player2WinsText = transform.Find("Player2WinsText")?.GetComponent<Text>();
            }
            
            // 見つからない場合は警告
            if (_currentRoundText == null)
                Debug.LogWarning("RoundCountView: 現在ラウンドテキストが見つかりません", this);
            if (_player1WinsText == null)
                Debug.LogWarning("RoundCountView: Player1勝利数テキストが見つかりません", this);
            if (_player2WinsText == null)
                Debug.LogWarning("RoundCountView: Player2勝利数テキストが見つかりません", this);
                
            Debug.Log($"RoundCountView: ラウンド数ビュー初期化完了 - Round: {_currentRoundText?.name}, P1: {_player1WinsText?.name}, P2: {_player2WinsText?.name}");
        }
        
        public async UniTask InitializeAsync()
        {
            await UniTask.DelayFrame(1);
            
            // 初期状態の設定
            UpdateCurrentRound(0);
            UpdatePlayer1Wins(0);
            UpdatePlayer2Wins(0);
        }
        
        public void Show()
        {
            if (!_isActive)
            {
                _isActive = true;
                gameObject.SetActive(true);
                Debug.Log("RoundCountView: ラウンド数ビューを表示");
            }
        }
        
        public void Hide()
        {
            if (_isActive)
            {
                _isActive = false;
                gameObject.SetActive(false);
                Debug.Log("RoundCountView: ラウンド数ビューを非表示");
            }
        }
        
        public void UpdateCurrentRound(int currentRound)
        {
            if (_currentRoundText != null)
            {
                _currentRoundText.text = string.Format(_roundTextFormat, currentRound);
                Debug.Log($"RoundCountView: 現在ラウンド数を {currentRound} に更新");
            }
        }
        
        public void UpdatePlayer1Wins(int wins)
        {
            if (_player1WinsText != null)
            {
                _player1WinsText.text = string.Format(_winsTextFormat, wins);
                Debug.Log($"RoundCountView: Player1勝利数を {wins} に更新");
            }
        }
        
        public void UpdatePlayer2Wins(int wins)
        {
            if (_player2WinsText != null)
            {
                _player2WinsText.text = string.Format(_winsTextFormat, wins);
                Debug.Log($"RoundCountView: Player2勝利数を {wins} に更新");
            }
        }
        
        
        /// <summary>
        /// テスト用: すべてのテキストを更新
        /// </summary>
        [ContextMenu("Test Update All")]
        public void TestUpdateAll()
        {
            UpdateCurrentRound(3);
            UpdatePlayer1Wins(2);
            UpdatePlayer2Wins(1);
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