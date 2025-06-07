using UnityEngine;
using UnityEngine.UI;
using Complete.Interfaces;
using Complete.Input;
using UniRx;

namespace Complete
{
    /// <summary>
    /// TankShooting - リファクタリング版に移行
    /// 新しいSOLID原則に基づいた設計を使用
    /// </summary>
    public class TankShooting : MonoBehaviour
    {
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

        private IInputHandler _inputHandler;
        private float m_CurrentLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired;
        private CompositeDisposable _disposables;


        private void Start()
        {
            // 入力ハンドラーを設定
            _inputHandler = new PlayerInputHandler(m_PlayerNumber);
            
            // チャージスピードを計算
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }

        private void OnEnable()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;

            // UniRx購読セットアップ
            _disposables = new CompositeDisposable();

            // 毎フレーム入力および射撃処理
            Observable.EveryUpdate()
                   .Subscribe(_ =>
                   {
                       if (_inputHandler == null) return;
                       _inputHandler.UpdateInput();
                       HandleShootingInput();
                   })
                   .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables?.Dispose();
        }

        private void HandleShootingInput()
        {
            // スライダーのデフォルト値を設定
            m_AimSlider.value = m_MinLaunchForce;

            // 最大力に達しており、まだ発射していない場合
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
            // 射撃ボタンが押された瞬間
            else if (_inputHandler.FireButtonDown)
            {
                StartCharging();
            }
            // 射撃ボタンが押されている間（まだ発射していない）
            else if (_inputHandler.FireButtonHeld && !m_Fired)
            {
                ContinueCharging();
            }
            // 射撃ボタンが離された瞬間（まだ発射していない）
            else if (_inputHandler.FireButtonUp && !m_Fired)
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
            // 発射力を増加
            m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
            m_AimSlider.value = m_CurrentLaunchForce;
        }


        private void Fire()
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
        }
    }
}