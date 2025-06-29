using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Complete.UI.MVP
{
    /// <summary>
    /// Health HUD用のViewクラス
    /// HP表示UIの操作を担当
    /// </summary>
    public class HealthHUDView : MonoBehaviour, IHealthHUDView
    {
        [Header("UI Components")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Image _fillImage;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _fullHealthColor = Color.green;
        [SerializeField] private Color _criticalHealthColor = Color.yellow;
        [SerializeField] private Color _zeroHealthColor = Color.red;
        
        [Header("Animation Settings")]
        [SerializeField] private float _animationDuration = 0.2f;
        [SerializeField] private bool _enableCriticalAnimation = true;
        
        private bool _isActive = true;
        private bool _isCritical = false;
        private Coroutine _criticalAnimationCoroutine;
        
        public bool IsActive => _isActive;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            if (_healthSlider == null)
            {
                _healthSlider = GetComponentInChildren<Slider>();
                if (_healthSlider == null)
                {
                    Debug.LogError("HealthHUDView: ヘルススライダーが見つかりません!", this);
                    return;
                }
            }
            
            if (_fillImage == null && _healthSlider.fillRect != null)
            {
                _fillImage = _healthSlider.fillRect.GetComponent<Image>();
            }
            
            if (_fillImage == null)
            {
                Debug.LogWarning("HealthHUDView: フィルイメージが見つかりません。色変更が無効化されます。", this);
            }
            
            Debug.Log($"HealthHUDView: ヘルスHUD初期化完了 - スライダー: {_healthSlider?.name}, フィル: {_fillImage?.name}");
        }
        
        public async UniTask InitializeAsync()
        {
            await UniTask.DelayFrame(1);
            
            // 初期状態の設定
            if (_healthSlider != null)
            {
                _healthSlider.value = 1f;
            }
            
            if (_fillImage != null)
            {
                _fillImage.color = _fullHealthColor;
            }
        }
        
        public void Show()
        {
            if (!_isActive)
            {
                _isActive = true;
                gameObject.SetActive(true);
            }
        }
        
        public void Hide()
        {
            if (_isActive)
            {
                _isActive = false;
                gameObject.SetActive(false);
            }
        }
        
        public void UpdateHealthValue(float normalizedHealth)
        {
            Debug.Log($"HealthHUDView: ヘルス値更新: {normalizedHealth}");
            
            if (_healthSlider == null) 
            {
                Debug.LogError("HealthHUDView: ヘルススライダーがnullです!");
                return;
            }
            
            float clampedValue = Mathf.Clamp01(normalizedHealth);
            Debug.Log($"HealthHUDView: クランプ値: {clampedValue}, 現在のスライダー値: {_healthSlider.value}");
            
            // スムーズなアニメーション
            if (_animationDuration > 0)
            {
                Debug.Log($"HealthHUDView: アニメーション使用 (継続時間: {_animationDuration}秒)");
                _ = AnimateHealthValue(clampedValue);
            }
            else
            {
                Debug.Log($"HealthHUDView: スライダー値直接設定: {clampedValue}");
                _healthSlider.value = clampedValue;
                Debug.Log($"HealthHUDView: 設定後スライダー値: {_healthSlider.value}");
            }
        }
        
        private async UniTask AnimateHealthValue(float targetValue)
        {
            if (_healthSlider == null) return;
            
            float startValue = _healthSlider.value;
            float elapsedTime = 0f;
            
            while (elapsedTime < _animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / _animationDuration;
                _healthSlider.value = Mathf.Lerp(startValue, targetValue, t);
                await UniTask.NextFrame();
            }
            
            _healthSlider.value = targetValue;
        }
        
        public void UpdateHealthColor(Color color)
        {
            Debug.Log($"HealthHUDView: ヘルス色更新: {color}");
            
            if (_fillImage != null)
            {
                Debug.Log($"HealthHUDView: フィルイメージ色設定: {color}");
                _fillImage.color = color;
                Debug.Log($"HealthHUDView: 設定後フィルイメージ色: {_fillImage.color}");
            }
            else
            {
                Debug.LogWarning("HealthHUDView: フィルイメージがnullです!");
            }
        }
        
        /// <summary>
        /// テスト用: スライダーを直接設定
        /// </summary>
        public void TestSliderUpdate(float value)
        {
            Debug.Log($"HealthHUDView: ヘルススライダーテスト: {value}");
            if (_healthSlider != null)
            {
                _healthSlider.value = value;
                Debug.Log($"HealthHUDView: テストスライダー値設定: {_healthSlider.value}");
            }
            else
            {
                Debug.LogError("HealthHUDView: テスト不可 - ヘルススライダーがnullです!");
            }
        }
        
        public void SetCriticalState(bool isCritical)
        {
            if (_isCritical == isCritical) return;
            
            _isCritical = isCritical;
            
            if (_isCritical && _enableCriticalAnimation)
            {
                StartCriticalAnimation();
            }
            else
            {
                StopCriticalAnimation();
            }
        }
        
        public void SetDeathState()
        {
            StopCriticalAnimation();
            
            if (_healthSlider != null)
            {
                _healthSlider.value = 0f;
            }
            
            if (_fillImage != null)
            {
                _fillImage.color = _zeroHealthColor;
            }
        }
        
        private void StartCriticalAnimation()
        {
            StopCriticalAnimation();
            _criticalAnimationCoroutine = StartCoroutine(CriticalAnimationCoroutine());
        }
        
        private void StopCriticalAnimation()
        {
            if (_criticalAnimationCoroutine != null)
            {
                StopCoroutine(_criticalAnimationCoroutine);
                _criticalAnimationCoroutine = null;
            }
        }
        
        private System.Collections.IEnumerator CriticalAnimationCoroutine()
        {
            while (_isCritical && _fillImage != null)
            {
                // 点滅効果
                _fillImage.color = _criticalHealthColor;
                yield return new WaitForSeconds(0.3f);
                
                _fillImage.color = _zeroHealthColor;
                yield return new WaitForSeconds(0.3f);
            }
        }
        
        /// <summary>
        /// 正規化されたHP値に基づいて適切な色を計算
        /// </summary>
        public Color GetHealthColor(float normalizedHealth)
        {
            if (normalizedHealth <= 0f)
                return _zeroHealthColor;
            else if (normalizedHealth <= 0.25f)
                return Color.Lerp(_zeroHealthColor, _criticalHealthColor, normalizedHealth / 0.25f);
            else
                return Color.Lerp(_criticalHealthColor, _fullHealthColor, (normalizedHealth - 0.25f) / 0.75f);
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        public void Dispose()
        {
            StopCriticalAnimation();
        }
    }
}