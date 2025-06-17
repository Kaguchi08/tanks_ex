using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Cysharp.Threading.Tasks;
using System;
using Complete.UI.HUD;

namespace Complete
{
    public class HUDManager : MonoBehaviour, IHUDManager
    {
        [SerializeField] private Canvas m_HUDCanvas;
        [SerializeField] private HealthHUD m_HealthHUD; // 直接参照用
        
        private readonly Dictionary<Type, IHUDElement> _hudElements = new Dictionary<Type, IHUDElement>();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<bool> _onVisibilityChangedSubject = new Subject<bool>();
        
        private bool _isVisible = true;
        
        public IObservable<bool> OnVisibilityChanged => _onVisibilityChangedSubject.AsObservable();
        
        private void Awake()
        {
            InitializeSync();
        }
        
        private void InitializeSync()
        {
            // 直接参照があるHealthHUDを最優先で登録
            if (m_HealthHUD != null)
            {
                RegisterHUDElement(m_HealthHUD);
                Debug.Log($"直接参照でHealthHUDを登録: {m_HealthHUD.GetType().Name}");
            }
            
            // HUDCanvasからHUD要素を検出・登録
            var hudElements = new List<HUDBase>();
            
            // 自分自身の子から検索
            hudElements.AddRange(GetComponentsInChildren<HUDBase>(true));
            
            // HUDCanvasが設定されている場合、そこからも検索
            if (m_HUDCanvas != null)
            {
                hudElements.AddRange(m_HUDCanvas.GetComponentsInChildren<HUDBase>(true));
            }
            
            // シーン全体から検索（フォールバック）
            if (hudElements.Count == 0)
            {
                hudElements.AddRange(FindObjectsOfType<HUDBase>(true));
            }
            
            foreach (var hud in hudElements)
            {
                RegisterHUDElement(hud);
            }
            
            Debug.Log($"HUDManager初期化完了: {_hudElements.Count}個のHUD要素を登録");
            
            // 登録されたHUD要素の詳細をログ出力
            foreach (var kvp in _hudElements)
            {
                Debug.Log($"  登録済みHUD: {kvp.Key.Name} - {kvp.Value.GetType().Name}");
            }
        }
        
        public async UniTask InitializeAsync()
        {
            // 非同期初期化が必要な場合のメソッド
            var hudElements = GetComponentsInChildren<HUDBase>(true);
            
            var initializeTasks = hudElements.Select(async hud =>
            {
                await hud.InitializeAsync();
            });
            
            await UniTask.WhenAll(initializeTasks);
            
            Debug.Log($"HUDManager非同期初期化完了");
        }
        
        public void RegisterHUDElement<T>(T element) where T : IHUDElement
        {
            var type = element.GetType(); // 実際の型を使用
            if (!_hudElements.ContainsKey(type))
            {
                _hudElements[type] = element;
                
                // Show/Hide イベントの購読
                element.OnShow
                    .Subscribe(_ => Debug.Log($"HUD要素が表示されました: {type.Name}"))
                    .AddTo(_disposables);
                    
                element.OnHide
                    .Subscribe(_ => Debug.Log($"HUD要素が非表示になりました: {type.Name}"))
                    .AddTo(_disposables);
                    
                Debug.Log($"HUD要素を登録しました: {type.Name}");
            }
            else
            {
                Debug.Log($"HUD要素は既に登録済みです: {type.Name}");
            }
        }
        
        public void UnregisterHUDElement<T>(T element) where T : IHUDElement
        {
            var type = element.GetType(); // 実際の型を使用
            if (_hudElements.ContainsKey(type))
            {
                _hudElements.Remove(type);
                Debug.Log($"HUD要素の登録を解除しました: {type.Name}");
            }
        }
        
        public T GetHUDElement<T>() where T : class, IHUDElement
        {
            var targetType = typeof(T);
            
            // 型が完全一致するものを探す
            if (_hudElements.TryGetValue(targetType, out var element))
            {
                return element as T;
            }
            
            // 継承関係にあるものを探す
            foreach (var kvp in _hudElements)
            {
                if (targetType.IsAssignableFrom(kvp.Key))
                {
                    return kvp.Value as T;
                }
            }
            
            Debug.LogWarning($"HUD要素が見つかりませんでした: {targetType.Name}");
            return null;
        }
        
        public void ShowAll()
        {
            if (!_isVisible)
            {
                _isVisible = true;
                
                if (m_HUDCanvas != null)
                {
                    m_HUDCanvas.gameObject.SetActive(true);
                }
                
                foreach (var element in _hudElements.Values)
                {
                    element?.Show();
                }
                
                _onVisibilityChangedSubject.OnNext(true);
            }
        }
        
        public void HideAll()
        {
            if (_isVisible)
            {
                _isVisible = false;
                
                foreach (var element in _hudElements.Values)
                {
                    element?.Hide();
                }
                
                if (m_HUDCanvas != null)
                {
                    m_HUDCanvas.gameObject.SetActive(false);
                }
                
                _onVisibilityChangedSubject.OnNext(false);
            }
        }
        
        public void SetPlayerHealthProvider(IHealthProvider healthProvider)
        {
            if (healthProvider == null)
            {
                Debug.LogWarning("HUDManager: HealthProvider is null");
                return;
            }
            
            // 直接的な方法でHealthHUDを検索
            HealthHUD healthHUD = null;
            foreach (var kvp in _hudElements)
            {
                if (kvp.Value is HealthHUD hud)
                {
                    healthHUD = hud;
                    break;
                }
            }
            
            // フォールバック: GetComponentでも検索
            if (healthHUD == null)
            {
                healthHUD = GetComponentInChildren<HealthHUD>();
            }
            
            if (healthHUD != null)
            {
                healthHUD.SetHealthProvider(healthProvider);
            }
            else
            {
                Debug.LogError("HUDManager: HealthHUDが見つかりませんでした");
            }
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        public void Dispose()
        {
            _disposables?.Dispose();
            _onVisibilityChangedSubject?.Dispose();
            
            foreach (var element in _hudElements.Values)
            {
                element?.Dispose();
            }
            
            _hudElements.Clear();
        }
    }
}