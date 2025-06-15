using UnityEngine;
using UnityEngine.UI;
using Complete.Interfaces;
using UniRx;
using System;
using Tanks.Shared;

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
        private ReactiveProperty<float> m_CurrentLaunchForce = new ReactiveProperty<float>();
        private float m_ChargeSpeed;
        private bool m_Fired;
        private CompositeDisposable _disposables;

        private NetworkManager _networkManager;
        private TankManager _tankManager;

        public void Setup(IInputProvider inputProvider)
        {
            _inputProvider = inputProvider;
        }

        public void Initialize(TankManager tankManager)
        {
            _tankManager = tankManager;
            _networkManager = FindObjectOfType<NetworkManager>();
        }

        private void Awake()
        {
            // チャージスピードを計算
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }

        private void OnEnable()
        {
            m_CurrentLaunchForce.Value = m_MinLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;

            _disposables = new CompositeDisposable();

            // スライダーの値をReactivePropertyで自動更新
            m_CurrentLaunchForce
                .Subscribe(force => m_AimSlider.value = force)
                .AddTo(_disposables);

            // 最大チャージ時の自動発射
            m_CurrentLaunchForce
                .Where(force => force >= m_MaxLaunchForce && !m_Fired)
                .Subscribe(_ => Fire())
                .AddTo(_disposables);

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

        private void StartCharging()
        {
            m_Fired = false;
            m_CurrentLaunchForce.Value = m_MinLaunchForce;

            // チャージ音を再生
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }

        private void ContinueCharging()
        {
            // 発射力を増加させる
            m_CurrentLaunchForce.Value += m_ChargeSpeed * Time.deltaTime;
        }

        public void Fire()
        {
            m_Fired = true;

            // シェルのインスタンスを作成
            var shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
            var shellExplosion = shellInstance.GetComponent<ShellExplosion>();

            Vector3 fireDirection = m_FireTransform.forward;
            float currentForce = m_CurrentLaunchForce.Value;

            // シェルに速度を設定
            shellInstance.velocity = currentForce * fireDirection;

            // ネットワークモードの場合、発射情報を同期
            if (_networkManager != null && _networkManager.IsNetworkMode && _tankManager != null)
            {
                var fireData = new ShellFireData
                {
                    PlayerID = _tankManager.m_PlayerID,
                    PositionX = m_FireTransform.position.x,
                    PositionY = m_FireTransform.position.y,
                    PositionZ = m_FireTransform.position.z,
                    DirectionX = fireDirection.x,
                    DirectionY = fireDirection.y,
                    DirectionZ = fireDirection.z,
                    Force = currentForce
                };

                _ = _networkManager.FireAsync(fireData);
            }

            // 発射音を再生
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();

            // 発射力をリセット
            m_CurrentLaunchForce.Value = m_MinLaunchForce;
            
            // 発射イベントを通知
            _onFiredSubject.OnNext(Unit.Default);
        }

        public void FireFromNetwork(ShellFireData fireData)
        {
            // ネットワークから受信した発射データで砲弾を生成
            var position = new Vector3(fireData.PositionX, fireData.PositionY, fireData.PositionZ);
            var direction = new Vector3(fireData.DirectionX, fireData.DirectionY, fireData.DirectionZ);

            var shellInstance = Instantiate(m_Shell, position, Quaternion.LookRotation(direction)) as Rigidbody;
            var shellExplosion = shellInstance.GetComponent<ShellExplosion>();
            
            // ネットワーク生成の砲弾であることをマーク
            if (shellExplosion != null)
            {
                shellExplosion.SetFromNetwork(true);
            }

            // シェルに速度を設定
            shellInstance.velocity = fireData.Force * direction;

            // 発射音を再生
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
        }
    }
}