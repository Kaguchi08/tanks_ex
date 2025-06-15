using UnityEngine;
using Complete.Interfaces;
using UniRx;
using System;

namespace Complete
{
    /// <summary>
    /// TankMovement - リファクタリング版に移行
    /// 新しいSOLID原則に基づいた設計を使用
    /// </summary>
    public class TankMovement : MonoBehaviour
    {

        [Header("Movement Settings")]
        public float m_Speed = 12f;
        public float m_TurnSpeed = 180f;

        [Header("Audio Settings")]
        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling;
        public AudioClip m_EngineDriving;
        public float m_PitchRange = 0.2f;

        private IInputProvider _inputProvider;
        private Rigidbody m_Rigidbody;
        private float m_OriginalPitch;
        private ParticleSystem[] m_particleSystems;
        private CompositeDisposable _disposables;

        private float _movementInputValue;
        private float _turnInputValue;
        private bool _isPhysicsOverride = false;
        private float _physicsOverrideEndTime = 0f;

        public void Setup(IInputProvider inputProvider)
        {
            _inputProvider = inputProvider;
        }

        public void SetPhysicsOverride(float duration = 2f)
        {
            _isPhysicsOverride = true;
            _physicsOverrideEndTime = Time.time + duration;
        }

        public bool IsPhysicsOverride => _isPhysicsOverride && Time.time < _physicsOverrideEndTime;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_OriginalPitch = m_MovementAudio.pitch;
        }

        private void OnEnable()
        {
            m_Rigidbody.isKinematic = false;
            
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in m_particleSystems)
            {
                ps.Play();
            }

            _disposables = new CompositeDisposable();

            if (_inputProvider != null)
            {
                _inputProvider.MovementInput
                    .Subscribe(value => _movementInputValue = value)
                    .AddTo(_disposables);

                _inputProvider.TurnInput
                    .Subscribe(value => _turnInputValue = value)
                    .AddTo(_disposables);
                
                // エンジン音の更新
                _inputProvider.MovementInput.Merge(_inputProvider.TurnInput.Select(x => Mathf.Abs(x)))
                    .Select(v => Mathf.Abs(v) > 0.1f)
                    .DistinctUntilChanged()
                    .Subscribe(isMoving =>
                    {
                        var clip = isMoving ? m_EngineDriving : m_EngineIdling;
                        PlayEngineClip(clip);
                    })
                    .AddTo(_disposables);
            }
            
            // FixedUpdate相当で移動・回転処理
            Observable.EveryFixedUpdate()
                .Subscribe(_ =>
                {
                    // 物理演算オーバーライド状態を更新
                    if (_isPhysicsOverride && Time.time >= _physicsOverrideEndTime)
                    {
                        _isPhysicsOverride = false;
                    }

                    Move(_movementInputValue);
                    Turn(_turnInputValue);
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


        private void PlayEngineClip(AudioClip clip)
        {
            m_MovementAudio.clip = clip;
            m_MovementAudio.pitch = UnityEngine.Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
            m_MovementAudio.Play();
        }
    }
}