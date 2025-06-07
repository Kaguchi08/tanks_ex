using System;
using UnityEngine;
using Complete.Interfaces;

namespace Complete
{
    /// <summary>
    /// TankManager - 既存の互換性を保持しつつ、新しい設計に対応
    /// ITankControllerインターフェースに適合させることで段階的な移行を可能にする
    /// </summary>
    [Serializable]
    public class TankManager : ITankController
    {
        [Header("Tank Settings")]
        public Color m_PlayerColor;
        public Transform m_SpawnPoint;

        [HideInInspector] public string m_ColoredPlayerText;
        [HideInInspector] public GameObject m_Instance;
        [HideInInspector] public int m_Wins;

        public int PlayerNumber { get; set; }

        private TankMovement m_Movement;
        private TankShooting m_Shooting;
        private GameObject m_CanvasGameObject;


        /// <summary>
        /// タンクをセットアップ
        /// </summary>
        /// <param name="tankInstance">タンクのインスタンス</param>
        public void Setup(GameObject tankInstance)
        {
            m_Instance = tankInstance;
            
            // コンポーネントを取得
            m_Movement = m_Instance.GetComponent<TankMovement>();
            m_Shooting = m_Instance.GetComponent<TankShooting>();
            m_CanvasGameObject = m_Instance.GetComponentInChildren<Canvas>().gameObject;

            // プレイヤー番号を設定
            m_Movement.m_PlayerNumber = PlayerNumber;
            m_Shooting.m_PlayerNumber = PlayerNumber;

            // プレイヤーテキストを作成
            m_ColoredPlayerText = "<color=#" + ColorUtility.ToHtmlStringRGB(m_PlayerColor) + ">PLAYER " + PlayerNumber + "</color>";

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
    }
}