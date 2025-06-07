using UnityEngine;
using Complete.Interfaces;
using Complete.Input;
using UniRx;

namespace Complete
{
    /// <summary>
    /// TankMovement - リファクタリング版に移行
    /// 新しいSOLID原則に基づいた設計を使用
    /// </summary>
    public class TankMovement : MonoBehaviour
    {
        [Header("Player Settings")]
        public int m_PlayerNumber = 1;

        [Header("Movement Settings")]
        public float m_Speed = 12f;
        public float m_TurnSpeed = 180f;

        [Header("Audio Settings")]
        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling;
        public AudioClip m_EngineDriving;
        public float m_PitchRange = 0.2f;

        private IInputHandler _inputHandler;
        private Rigidbody m_Rigidbody;
        private float m_OriginalPitch;
        private ParticleSystem[] m_particleSystems;
        private CompositeDisposable _disposables;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_OriginalPitch = m_MovementAudio.pitch;
        }

        private void Start()
        {
            // 入力ハンドラーを設定
            _inputHandler = new PlayerInputHandler(m_PlayerNumber);
        }

        private void OnEnable()
        {
            m_Rigidbody.isKinematic = false;
            
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in m_particleSystems)
            {
                ps.Play();
            }

            // UniRx購読セットアップ
            _disposables = new CompositeDisposable();

            // 毎フレーム入力更新 & エンジン音
            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    _inputHandler?.UpdateInput();
                    UpdateEngineAudio();
                })
                .AddTo(_disposables);

            // FixedUpdate相当で移動・回転処理
            Observable.EveryFixedUpdate()
                .Subscribe(_ =>
                {
                    if (_inputHandler != null)
                    {
                        Move(_inputHandler.MovementInput);
                        Turn(_inputHandler.TurnInput);
                    }
                })
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            m_Rigidbody.isKinematic = true;
            
            foreach (var ps in m_particleSystems)
            {
                ps.Stop();
            }

            _disposables?.Dispose();
        }

        private void Update()
        {
            // Update/FixedUpdate は UniRx に置き換え済み
        }

        private void Move(float movementInput)
        {
            Vector3 movement = transform.forward * movementInput * m_Speed * Time.deltaTime;
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }

        private void Turn(float turnInput)
        {
            float turn = turnInput * m_TurnSpeed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }

        private void UpdateEngineAudio()
        {
            if (_inputHandler == null) return;

            bool isMoving = Mathf.Abs(_inputHandler.MovementInput) > 0.1f || 
                           Mathf.Abs(_inputHandler.TurnInput) > 0.1f;

            if (!isMoving && m_MovementAudio.clip == m_EngineDriving)
            {
                PlayEngineClip(m_EngineIdling);
            }
            else if (isMoving && m_MovementAudio.clip == m_EngineIdling)
            {
                PlayEngineClip(m_EngineDriving);
            }
        }

        private void PlayEngineClip(AudioClip clip)
        {
            m_MovementAudio.clip = clip;
            m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
            m_MovementAudio.Play();
        }
    }
}