using UnityEngine;
using UniRx;
using System;

namespace Complete
{
    public class UIDirectionControl : MonoBehaviour
    {
        // This class is used to make sure world space UI
        // elements such as the health bar face the correct direction.

        public bool m_UseRelativeRotation = true;       // Use relative rotation should be used for this gameobject?


        private Quaternion m_RelativeRotation;          // The local rotatation at the start of the scene.
        private IDisposable _subscription;


        private void Start ()
        {
            m_RelativeRotation = transform.parent.localRotation;

            // UniRx で毎フレーム向きを更新
            _subscription = Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    if (m_UseRelativeRotation)
                        transform.rotation = m_RelativeRotation;
                });
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
    }
}