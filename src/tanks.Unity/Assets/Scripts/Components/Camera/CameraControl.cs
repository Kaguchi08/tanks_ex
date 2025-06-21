using UnityEngine;
using Complete.Input; // 2つ目のファイルで追加された名前空間

namespace Complete
{
    /// <summary>
    /// カメラ制御
    /// - 俯瞰視点 (RTS)
    /// - TPS視点（プレイヤー追従）
    /// </summary>
    public class CameraControl : MonoBehaviour
    {
        #region 一般設定 --------------------------------------------------
        public float m_DampTime = 0.2f;            // カメラが目標位置へ追従するおおよその時間
        public float m_ScreenEdgeBuffer = 4f;      // 画面端と一番端のターゲットとのバッファ
        public float m_MinSize = 6.5f;             // カメラの最小オーソサイズ
        [HideInInspector] public Transform[] m_Targets; // 追従対象のトランスフォーム配列
        #endregion

        #region TPS カメラ設定 -------------------------------------------
        [Header("TPS Camera Settings")]
        public bool m_UseTpsCamera = true;   // TPS カメラを使用するか
        public float m_TpsHeight = 5.0f;   // 高さオフセット
        public float m_TpsDistance = 5.0f;   // 後方距離
        public float m_TpsDampTime = 0.05f;  // TPS 追従ダンパー
        public float m_TpsFieldOfView = 10f;    // TPS FOV
        public float m_TpsLookAheadDistance = 50.0f;  // プレイヤー前方を注視させる距離

        #endregion

        #region クリッピングプレーン設定（TPS のみ有効）---------------
        [Header("Clipping Planes Settings")]
        public float m_NearClipPlane = 40f;          // Near 面
        public float m_FarClipPlane = 1000f;        // Far 面
        public bool m_ApplyClippingPlanesInRealtime = true; // 実行時に値変更を反映するか
        #endregion

        #region 障害物透明化設定 ----------------------------------------
        [Header("Obstacle Transparency")]
        public bool m_UseObstacleTransparency = true; // プレイヤーとカメラの間の障害物を透明化するか
        #endregion

        #region 内部変数 -------------------------------------------------
        private Camera m_Camera;               // 実カメラ
        private float m_ZoomSpeed;            // オーソサイズの SmoothDamp 用速度
        private Vector3 m_MoveVelocity;         // 位置の SmoothDamp 用速度
        private Vector3 m_DesiredPosition;      // RTS での目標位置

        private bool m_IsTpsActive = false;  // TPS かどうか
        private Vector3 m_TpsVelocity;          // TPS 位置用 SmoothDamp 速度
        private Vector3 m_TpsDesiredPosition;   // TPS 目標位置
        private Quaternion m_OriginalRotation;  // RTS 時の元の回転
        private float m_OriginalFieldOfView; // RTS 時の元の FOV
        private Vector3 m_LastTargetForward = Vector3.forward; // 前回ターゲットの forward（急回転ノイズ抑制）
        private CameraObstacleHandler m_ObstacleHandler; // 障害物透明化ハンドラ
        #endregion

        #region Unity コールバック --------------------------------------
        private void Awake()
        {
            m_Camera = GetComponentInChildren<Camera>();
            m_OriginalRotation = transform.rotation;
            m_OriginalFieldOfView = m_Camera.fieldOfView;

            // 障害物透明化ハンドラを取得／追加
            m_ObstacleHandler = GetComponent<CameraObstacleHandler>();
            if (m_ObstacleHandler == null && m_UseObstacleTransparency)
                m_ObstacleHandler = gameObject.AddComponent<CameraObstacleHandler>();
        }

        private void FixedUpdate()
        {
            if (m_IsTpsActive && m_UseTpsCamera && m_Targets.Length > 0)
            {
                // TPS 視点
                MoveTps();
            }
            else
            {
                // 俯瞰視点
                Move();
                Zoom();
            }
        }

        private void Update()
        {
            // 実行中に Near/Far を変更したい場合
            if (m_ApplyClippingPlanesInRealtime && m_IsTpsActive)
                ApplyClippingPlanes();
        }
        #endregion

        #region 公開 API -------------------------------------------------
        /// <summary>
        /// TPS カメラの有効／無効を切り替える
        /// </summary>
        public void ActivateTpsCamera(bool activate)
        {
            m_IsTpsActive = activate && m_UseTpsCamera;

            if (m_IsTpsActive)
            {
                // パースペクティブ設定
                m_Camera.orthographic = false;
                m_Camera.fieldOfView = m_TpsFieldOfView;
                ApplyClippingPlanes();

                Transform targetTank = GetMyTank();
                if (targetTank != null)
                {
                    m_LastTargetForward = targetTank.forward;

                    transform.position = targetTank.position
                                         + m_LastTargetForward * m_TpsDistance
                                         + Vector3.up * m_TpsHeight
                                         + Vector3.Cross(Vector3.up, m_LastTargetForward).normalized;

                    m_TpsVelocity = Vector3.zero;

                    // プレイヤー前方を注視
                    Vector3 lookAtPos = targetTank.position + targetTank.forward * m_TpsLookAheadDistance;
                    Vector3 dir = lookAtPos - transform.position;
                    if (dir != Vector3.zero)
                        transform.rotation = Quaternion.LookRotation(dir);

                    // 障害物透明化有効化
                    if (m_ObstacleHandler != null && m_UseObstacleTransparency)
                        m_ObstacleHandler.SetTarget(targetTank, true);
                }
            }
            else
            {
                // 俯瞰視点に戻す
                m_Camera.orthographic = true;
                m_Camera.fieldOfView = m_OriginalFieldOfView;
                SetStartPositionAndSize();

                if (m_ObstacleHandler != null)
                    m_ObstacleHandler.SetTarget(null, false);
            }
        }
        #endregion

        #region TPS 関連メソッド -----------------------------------------
        private void MoveTps()
        {
            Transform targetTank = GetMyTank();
            if (targetTank == null) return;

            // 大きく向きが変わったときのみ更新（ノイズ低減）
            if (Vector3.Angle(targetTank.forward, m_LastTargetForward) > 5f)
                m_LastTargetForward = targetTank.forward;

            m_TpsDesiredPosition = targetTank.position
                                   + m_LastTargetForward * m_TpsDistance
                                   + Vector3.up * m_TpsHeight
                                   + Vector3.Cross(Vector3.up, m_LastTargetForward).normalized;

            transform.position = Vector3.SmoothDamp(transform.position, m_TpsDesiredPosition, ref m_TpsVelocity, m_TpsDampTime);

            // プレイヤー前方を注視
            Vector3 lookPos = targetTank.position + targetTank.forward * m_TpsLookAheadDistance;
            Vector3 lookDir = lookPos - transform.position;
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, m_TpsDampTime * 5f);
            }
        }

        /// <summary>
        /// ネットワークモード対応で「自分のタンク」を取得（2つ目のファイルから追加）
        /// </summary>
        private Transform GetMyTank()
        {
            if (m_Targets == null || m_Targets.Length == 0)
                return null;

            // GameManager があればネットワークモード判定
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null && gm.m_UseNetworkMode)
            {
                foreach (var tank in gm.m_Tanks)
                {
                    if (tank.m_Instance != null && tank.InputProvider is LocalInputProvider)
                        return tank.m_Instance.transform;
                }
            }

            // 見つからなければ先頭ターゲット
            return m_Targets[0];
        }
        #endregion

        #region RTS 関連メソッド -----------------------------------------
        private void Move()
        {
            FindAveragePosition();
            transform.position = Vector3.SmoothDamp(transform.position, m_DesiredPosition, ref m_MoveVelocity, m_DampTime);

            // 俯瞰視点時は元の回転に戻す
            if (!m_IsTpsActive)
                transform.rotation = m_OriginalRotation;
        }

        private void Zoom()
        {
            float size = FindRequiredSize();
            m_Camera.orthographicSize = Mathf.SmoothDamp(m_Camera.orthographicSize, size, ref m_ZoomSpeed, m_DampTime);
        }

        private void FindAveragePosition()
        {
            Vector3 avg = Vector3.zero;
            int count = 0;
            foreach (var t in m_Targets)
            {
                if (!t.gameObject.activeSelf) continue;
                avg += t.position;
                count++;
            }
            if (count > 0) avg /= count;
            avg.y = transform.position.y; // 高さは固定
            m_DesiredPosition = avg;
        }

        private float FindRequiredSize()
        {
            Vector3 desiredLocal = transform.InverseTransformPoint(m_DesiredPosition);
            float size = 0f;
            foreach (var t in m_Targets)
            {
                if (!t.gameObject.activeSelf) continue;
                Vector3 targetLocal = transform.InverseTransformPoint(t.position);
                Vector3 diff = targetLocal - desiredLocal;
                size = Mathf.Max(size, Mathf.Abs(diff.y));
                size = Mathf.Max(size, Mathf.Abs(diff.x) / m_Camera.aspect);
            }
            size += m_ScreenEdgeBuffer;
            size = Mathf.Max(size, m_MinSize);
            return size;
        }

        /// <summary>
        /// 俯瞰視点の初期化（開始時や TPS 解除時）
        /// </summary>
        public void SetStartPositionAndSize()
        {
            m_IsTpsActive = false;
            m_Camera.orthographic = true;
            transform.rotation = m_OriginalRotation;

            ApplyClippingPlanes();

            FindAveragePosition();
            transform.position = m_DesiredPosition;
            m_Camera.orthographicSize = FindRequiredSize();
        }
        #endregion

        #region 補助メソッド ----------------------------------------------
        private void ApplyClippingPlanes()
        {
            if (m_Camera != null && m_IsTpsActive)
            {
                m_Camera.nearClipPlane = m_NearClipPlane;
                m_Camera.farClipPlane = m_FarClipPlane;
            }
        }
        #endregion
    }
}
