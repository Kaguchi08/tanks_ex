using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System;
using Complete.UI.HUD;

namespace Complete
{
    public abstract class HUDBase : MonoBehaviour, IHUDElement 
    {
        [SerializeField] protected bool m_IsActive = true;
        
        protected readonly CompositeDisposable _disposables = new CompositeDisposable();
        protected readonly Subject<Unit> _onShowSubject = new Subject<Unit>();
        protected readonly Subject<Unit> _onHideSubject = new Subject<Unit>();
        
        public bool IsActive => m_IsActive;
        public IObservable<Unit> OnShow => _onShowSubject.AsObservable();
        public IObservable<Unit> OnHide => _onHideSubject.AsObservable();
        
        protected virtual void Awake()
        {
            // 同期的な初期化
        }
        
        public virtual async UniTask InitializeAsync()
        {
            await UniTask.Yield();
        }
        
        public virtual void Show()
        {
            if (!m_IsActive)
            {
                m_IsActive = true;
                gameObject.SetActive(true);
                _onShowSubject.OnNext(Unit.Default);
            }
        }
        
        public virtual void Hide()
        {
            if (m_IsActive)
            {
                m_IsActive = false;
                gameObject.SetActive(false);
                _onHideSubject.OnNext(Unit.Default);
            }
        }
        
        public virtual void SetActive(bool active)
        {
            if (active)
                Show();
            else
                Hide();
        }
        
        protected virtual void OnDestroy()
        {
            Dispose();
        }
        
        public virtual void Dispose()
        {
            _disposables?.Dispose();
            _onShowSubject?.Dispose();
            _onHideSubject?.Dispose();
        }
    }
}