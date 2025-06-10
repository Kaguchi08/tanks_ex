using UnityEngine;
using UnityEngine.UI;
using Complete.Interfaces;
using UniRx;
using System;

namespace Complete
{
    /// <summary>
    /// TankShooting - リファクタリング版に移行
    /// 新しいSOLID原則に基づいた設計を使用
    /// </summary>
    public class TankShooting : MonoBehaviour
    {
        // 発射時のイベント (UniRx)
        readonly Subject<Unit> _onFiredSubject = new Subject<Unit>();
        public IObservable<Unit> OnFiredObservable => _onFiredSubject;
        [Header("Player Settings")]
        public int m_PlayerNumber = 1;

        [Header("Shooting Settings")]
        public Rigidbody m_Shell;
        public Transform m_FireTransform;
        public float m_MinLaunchForce = 15f;
        public float m_MaxLaunchForce = 30f;
        public float m_MaxChargeTime = 0.75f;

        [Header("UI")]
        public Slider m_AimSlider;

        [Header("Audio")]
        public AudioSource m_ShootingAudio;
        public AudioClip m_ChargingClip;
        public AudioClip m_FireClip;

        private IInputProvider _inputProvider;
        private float m_CurrentLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired;
        private CompositeDisposable _disposables;

        public void Setup(IInputProvider inputProvider)
        {
            _inputProvider = inputProvider;
        }

        private void Awake()
        {
            // チャージスピードを計算
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }

        private void OnEnable()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;

            _disposables = new CompositeDisposable();

            if (_inputProvider != null)
            {
                // ボタンの入力状態が変化した時だけ発火するストリームを作成
                // Skip(1)で最初の不要な通知を無視する
                _inputProvider.FireButton
                    .Skip(1)
                    .DistinctUntilChanged()
                    .Subscribe(isPressed =>
                    {
                        if (isPressed)
                        {
                            // ボタンが押された瞬間の処理 (チャージ開始)
                            StartCharging();
                        }
                        else
                        {
                            // ボタンが離された瞬間の処理 (発射)
                            // m_Firedフラグをチェックし、最大チャージで発射済みでなければ発射
                            if (!m_Fired)
                            {
                                Fire();
                            }
                        }
                    })
                    .AddTo(_disposables);

                // ボタンが押されている間、継続的にチャージする
                Observable.EveryUpdate()
                    .Where(_ => _inputProvider.FireButton.Value && !m_Fired)
                    .Subscribe(_ => ContinueCharging())
                    .AddTo(_disposables);
            }
        }

        private void OnDisable()
        {
            _disposables?.Dispose();
            // InputProviderのライフサイクルはGameManagerが管理するため、ここでは破棄しない
            // if (_inputProvider is IDisposable disposable)
            // {
            //     disposable.Dispose();
            // }
            // _inputProvider = null;
        }
        
        private void Start()
        {
            // GameManagerがSetupを呼び出すので、ここでは何もしない
        }
        
        private void Update()
        {
            // スライダーの値を更新
            m_AimSlider.value = m_CurrentLaunchForce;

            // チャージが最大に達し、まだ発射していない場合は自動で発射
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                Fire();
            }
        }

        private void StartCharging()
        {
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;

            // チャージ音を再生
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }

        private void ContinueCharging()
        {
            // 発射力を増加させる
            m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
        }

        public void Fire()
        {
            m_Fired = true;

            // シェルのインスタンスを作成
            Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

            // シェルに速度を設定
            shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;

            // 発射音を再生
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();

            // 発射力をリセット
            m_CurrentLaunchForce = m_MinLaunchForce;
            
            // 発射イベントを通知
            _onFiredSubject.OnNext(Unit.Default);
        }
    }
}