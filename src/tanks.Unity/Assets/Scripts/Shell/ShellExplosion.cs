using UnityEngine;
using Cysharp.Threading.Tasks;
using Tanks.Shared;

namespace Complete
{
    public class ShellExplosion : MonoBehaviour
    {
        public LayerMask m_TankMask;                        // Used to filter what the explosion affects, this should be set to "Players".
        public ParticleSystem m_ExplosionParticles;         // Reference to the particles that will play on explosion.
        public AudioSource m_ExplosionAudio;                // Reference to the audio that will play on explosion.
        public float m_MaxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank.
        public float m_ExplosionForce = 1000f;              // The amount of force added to a tank at the centre of the explosion.
        public float m_MaxLifeTime = 2f;                    // The time in seconds before the shell is removed.
        public float m_ExplosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected.

        private NetworkManager _networkManager;
        private bool _isFromNetwork = false;

        private void Awake()
        {
            _networkManager = FindObjectOfType<NetworkManager>();
        }

        private void Start ()
        {
            // UniTask でライフタイム管理
            _ = LifeTimeAsync();
        }

        private async UniTaskVoid LifeTimeAsync()
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(m_MaxLifeTime), cancellationToken: this.GetCancellationTokenOnDestroy());
            if (this != null)
            {
                Destroy(gameObject);
            }
        }


        public void SetFromNetwork(bool fromNetwork)
        {
            _isFromNetwork = fromNetwork;
        }

        private void OnTriggerEnter (Collider other)
        {
			// Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
            Collider[] colliders = Physics.OverlapSphere (transform.position, m_ExplosionRadius, m_TankMask);

            // Go through all the colliders...
            for (int i = 0; i < colliders.Length; i++)
            {
                // ... and find their rigidbody.
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody> ();

                // If they don't have a rigidbody, go on to the next collider.
                if (!targetRigidbody)
                    continue;

                // タンクマネージャーを取得してPlayerIDを特定
                var gameManager = FindObjectOfType<GameManager>();
                TankManager tankManager = null;
                if (gameManager != null)
                {
                    tankManager = GetTankManagerFromRigidbody(gameManager, targetRigidbody);
                }

                bool isMyTank = false;
                if (_networkManager != null && _networkManager.IsNetworkMode && tankManager != null)
                {
                    // 自分のタンクかどうかを判定
                    isMyTank = (tankManager.m_PlayerID == gameManager.GetMyPlayerID());
                }

                // ローカル砲弾でネットワークモードの場合の処理分岐
                if (_networkManager != null && _networkManager.IsNetworkMode && !_isFromNetwork)
                {
                    if (isMyTank)
                    {
                        // 自分のタンクへのダメージ・物理力はローカルで処理
                        // 物理演算オーバーライドを設定（位置同期を一時停止）
                        var movement = tankManager.GetTankMovement();
                        if (movement != null)
                        {
                            movement.SetPhysicsOverride(2f);
                        }
                        
                        targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);
                        
                        TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
                        if (targetHealth != null)
                        {
                            float damage = CalculateDamage(targetRigidbody.position);
                            targetHealth.TakeDamage(damage);
                        }
                    }
                    else if (tankManager != null)
                    {
                        // 相手のタンクへの効果はネットワーク経由で送信のみ（ローカル適用しない）
                        var explosionData = new ExplosionForceData
                        {
                            TargetPlayerID = tankManager.m_PlayerID,
                            ExplosionX = transform.position.x,
                            ExplosionY = transform.position.y,
                            ExplosionZ = transform.position.z,
                            Force = m_ExplosionForce,
                            Radius = m_ExplosionRadius,
                            Damage = CalculateDamage(targetRigidbody.position)
                        };
                        
                        _ = _networkManager.ApplyExplosionForceAsync(explosionData);
                    }
                }
                else if (!_isFromNetwork)
                {
                    // ローカルモードまたはネットワーク受信砲弾でない場合は通常処理
                    targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

                    TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
                    if (targetHealth != null)
                    {
                        float damage = CalculateDamage(targetRigidbody.position);
                        targetHealth.TakeDamage(damage);
                    }
                }
            }

            // Unparent the particles from the shell.
            m_ExplosionParticles.transform.parent = null;

            // Play the particle system.
            m_ExplosionParticles.Play();

            // Play the explosion sound effect.
            m_ExplosionAudio.Play();

            // Once the particles have finished, destroy the gameobject they are on.
            ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
            Destroy (m_ExplosionParticles.gameObject, mainModule.duration);

            // Destroy the shell.
            Destroy (gameObject);
        }


        private float CalculateDamage (Vector3 targetPosition)
        {
            // Create a vector from the shell to the target.
            Vector3 explosionToTarget = targetPosition - transform.position;

            // Calculate the distance from the shell to the target.
            float explosionDistance = explosionToTarget.magnitude;

            // Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

            // Calculate damage as this proportion of the maximum possible damage.
            float damage = relativeDistance * m_MaxDamage;

            // Make sure that the minimum damage is always 0.
            damage = Mathf.Max (0f, damage);

            return damage;
        }

        private TankManager GetTankManagerFromRigidbody(GameManager gameManager, Rigidbody rigidbody)
        {
            foreach (var tankManager in gameManager.m_Tanks)
            {
                if (tankManager.m_Instance != null && tankManager.m_Instance.GetComponent<Rigidbody>() == rigidbody)
                {
                    return tankManager;
                }
            }
            return null;
        }
    }
}