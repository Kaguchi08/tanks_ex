using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Complete
{
    public class TankHealth : MonoBehaviour
    {
        public float m_StartingHealth = 100f;               // The amount of health each tank starts with.
        public Slider m_Slider;                             // The slider to represent how much health the tank currently has.
        public Image m_FillImage;                           // The image component of the slider.
        public Color m_FullHealthColor = Color.green;       // The color the health bar will be when on full health.
        public Color m_ZeroHealthColor = Color.red;         // The color the health bar will be when on no health.
        public GameObject m_ExplosionPrefab;                // A prefab that will be instantiated in Awake, then used whenever the tank dies.
        
        
        private AudioSource m_ExplosionAudio;               // The audio source to play when the tank explodes.
        private ParticleSystem m_ExplosionParticles;        // The particle system the will play when the tank is destroyed.
        private UniRx.ReactiveProperty<float> _currentHealth; // リアクティブなヘルス値
        private bool m_Dead;                                // Has the tank been reduced beyond zero health yet?

        private NetworkManager _networkManager;
        private TankManager _tankManager;


        private void Awake ()
        {
            // ネットワークマネージャーを取得
            _networkManager = FindObjectOfType<NetworkManager>();

            // Instantiate the explosion prefab and get a reference to the particle system on it.
            m_ExplosionParticles = Instantiate (m_ExplosionPrefab).GetComponent<ParticleSystem> ();

            // Get a reference to the audio source on the instantiated prefab.
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource> ();

            // Disable the prefab so it can be activated when it's required.
            m_ExplosionParticles.gameObject.SetActive (false);

            // ReactiveProperty 初期化と購読設定
            _currentHealth = new UniRx.ReactiveProperty<float>(m_StartingHealth);

            // UI スライダー更新
            _currentHealth
                .Subscribe(value =>
                {
                    m_Slider.value = value;
                    m_FillImage.color = Color.Lerp (m_ZeroHealthColor, m_FullHealthColor, value / m_StartingHealth);
                })
                .AddTo(this);

            // 死亡判定
            _currentHealth
                .Where(value => value <= 0f && !m_Dead)
                .Subscribe(_ => OnDeath())
                .AddTo(this);
        }


        private void OnEnable()
        {
            // When the tank is enabled, reset the tank's health and whether or not it's dead.
            _currentHealth.Value = m_StartingHealth;
            m_Dead = false;

            // ReactivePropertyによってスライダーは自動更新
        }


        public void Initialize(TankManager tankManager)
        {
            _tankManager = tankManager;
        }

        public void TakeDamage (float amount)
        {
            // Reduce current health by the amount of damage done.
            _currentHealth.Value -= amount;

            // ネットワークモードの場合、自分のタンクのみヘルス情報を同期
            if (_networkManager != null && _networkManager.IsNetworkMode && _tankManager != null)
            {
                var gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null && _tankManager.m_PlayerID == gameManager.GetMyPlayerID())
                {
                    _ = _networkManager.UpdateHealthAsync(_tankManager.m_PlayerID, _currentHealth.Value);
                }
            }
        }

        public void SetHealthFromNetwork(float health)
        {
            // ネットワーク経由でのヘルス設定（同期用）
            _currentHealth.Value = health;
        }

        public void TakeDamageFromNetwork(float amount)
        {
            // ネットワーク経由でのダメージ（同期なし）
            _currentHealth.Value -= amount;
        }


        private void OnDeath ()
        {
            // Set the flag so that this function is only called once.
            m_Dead = true;

            // Move the instantiated explosion prefab to the tank's position and turn it on.
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive (true);

            // Play the particle system of the tank exploding.
            m_ExplosionParticles.Play ();

            // Play the tank explosion sound effect.
            m_ExplosionAudio.Play();

            // Turn the tank off.
            gameObject.SetActive (false);
        }
    }
}