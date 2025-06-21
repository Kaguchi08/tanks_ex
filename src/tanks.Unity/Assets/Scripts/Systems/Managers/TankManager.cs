using System;
using UnityEngine;
using Complete.Interfaces;
using Complete.Input;

namespace Complete
{
    /// <summary>
    /// TankManager - 既存の互換性を保持しつつ、新しい設計に対応
    /// ITankControllerインターフェースに適合させることで段階的な移行を可能にする
    /// </summary>
    public enum TankType
    {
        Player,
        AI
    }

    [Serializable]
    public class TankManager : ITankController
    {
        [Header("Tank Settings")]
        public TankType m_TankType = TankType.Player;
        public Color m_PlayerColor;
        public Transform m_SpawnPoint;
        [HideInInspector] public string m_PlayerName;

        [HideInInspector] public string m_ColoredPlayerText;
        [HideInInspector] public GameObject m_Instance;
        [HideInInspector] public int m_Wins;
        [HideInInspector] public int m_PlayerID; // ネットワーク用のプレイヤーID

        public IInputProvider InputProvider { get; private set; }

        private TankMovement m_Movement;
        private TankShooting m_Shooting;
        private TankHealth m_Health;
        private GameObject m_CanvasGameObject;


        /// <summary>
        /// タンクをセットアップ
        /// </summary>
        /// <param name="tankInstance">タンクのインスタンス</param>
        /// <param name="inputProvider">入力プロバイダー</param>
        /// <param name="playerNumber">プレイヤー番号</param>
        /// <param name="playerName">プレイヤー名</param>
        public void Setup(GameObject tankInstance, IInputProvider inputProvider, int playerNumber, string playerName)
        {
            m_Instance = tankInstance;
            InputProvider = inputProvider;
            m_PlayerName = playerName;
            m_PlayerID = playerNumber;
            
            // コンポーネントを取得
            m_Movement = m_Instance.GetComponent<TankMovement>();
            m_Shooting = m_Instance.GetComponent<TankShooting>();
            m_Health = m_Instance.GetComponent<TankHealth>();
            m_CanvasGameObject = m_Instance.GetComponentInChildren<Canvas>().gameObject;

            // 各コンポーネントにInputProviderを設定
            m_Movement.Setup(InputProvider);
            m_Shooting.Setup(InputProvider);
            
            // TankManagerへの参照を設定
            m_Shooting.Initialize(this);
            m_Health.Initialize(this);

            // プレイヤーテキストを作成
            m_ColoredPlayerText = "<color=#" + ColorUtility.ToHtmlStringRGB(m_PlayerColor) + ">" + m_PlayerName + "</color>";

            // マテリアルの色を設定
            SetTankColor();
        }

        public void Enable()
        {
            m_Movement.enabled = true;
            m_Shooting.enabled = true;
            m_CanvasGameObject.SetActive(true);
        }

        public void Disable()
        {
            m_Movement.enabled = false;
            m_Shooting.enabled = false;
            m_CanvasGameObject.SetActive(false);
        }

        public void Reset()
        {
            m_Instance.transform.position = m_SpawnPoint.position;
            m_Instance.transform.rotation = m_SpawnPoint.rotation;
            
            m_Instance.SetActive(false);
            m_Instance.SetActive(true);
        }

        /// <summary>
        /// タンクの色を設定
        /// </summary>
        private void SetTankColor()
        {
            MeshRenderer[] renderers = m_Instance.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.material.color = m_PlayerColor;
            }
        }

        public TankHealth GetTankHealth()
        {
            return m_Health;
        }

        public TankShooting GetTankShooting()
        {
            return m_Shooting;
        }

        public TankMovement GetTankMovement()
        {
            return m_Movement;
        }
    }
}