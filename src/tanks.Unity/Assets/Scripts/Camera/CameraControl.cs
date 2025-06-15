using UnityEngine;
using Complete.Input;

namespace Complete
{
    public class CameraControl : MonoBehaviour
    {
        public float m_DampTime = 0.2f;                 // Approximate time for the camera to refocus.
        public float m_ScreenEdgeBuffer = 4f;           // Space between the top/bottom most target and the screen edge.
        public float m_MinSize = 6.5f;                  // The smallest orthographic size the camera can be.
        [HideInInspector] public Transform[] m_Targets; // All the targets the camera needs to encompass.

        [Header("TPS Camera Settings")]
        public bool m_UseTpsCamera = true;              // TPSカメラを使用するかどうか
        public float m_TpsHeight = 1.0f;                // TPSカメラの高さ
        public float m_TpsDistance = 4.0f;              // TPSカメラのプレイヤーからの距離
        public float m_TpsDampTime = 0.05f;              // TPSカメラの遷移時間
        public float m_TpsFieldOfView = 10f;            // TPSカメラの視野角
        public float m_TpsOffsetX = 0f;                 // TPSカメラの横オフセット（負の値で左、正の値で右）
        
        [Header("Obstacle Transparency")]
        public bool m_UseObstacleTransparency = true;   // 障害物の透明化を使用するかどうか

        private Camera m_Camera;                        // Used for referencing the camera.
        private float m_ZoomSpeed;                      // Reference speed for the smooth damping of the orthographic size.
        private Vector3 m_MoveVelocity;                 // Reference velocity for the smooth damping of the position.
        private Vector3 m_DesiredPosition;              // The position the camera is moving towards.
        
        private bool m_IsTpsActive = false;             // TPSカメラが有効かどうか
        private Vector3 m_TpsVelocity;                  // TPSカメラの移動速度参照
        private Vector3 m_TpsDesiredPosition;           // TPSカメラの目標位置
        private Quaternion m_OriginalRotation;          // 元の回転を保持
        private float m_OriginalFieldOfView;            // 元の視野角を保持
        private Vector3 m_LastTargetForward = Vector3.forward; // 前回のプレイヤーの向き
        private CameraObstacleHandler m_ObstacleHandler; // 障害物透明化ハンドラー

        private void Awake ()
        {
            m_Camera = GetComponentInChildren<Camera>();
            m_OriginalRotation = transform.rotation;
            m_OriginalFieldOfView = m_Camera.fieldOfView;
            
            // 障害物透明化ハンドラーの取得またはコンポーネントの追加
            m_ObstacleHandler = GetComponent<CameraObstacleHandler>();
            if (m_ObstacleHandler == null && m_UseObstacleTransparency)
            {
                m_ObstacleHandler = gameObject.AddComponent<CameraObstacleHandler>();
            }
        }


        private void FixedUpdate ()
        {
            if (m_IsTpsActive && m_UseTpsCamera && m_Targets.Length > 0)
            {
                // TPS視点の場合、自分のタンクを追従
                MoveTps();
            }
            else
            {
                // 俯瞰視点の場合
                // Move the camera towards a desired position.
                Move();

                // Change the size of the camera based.
                Zoom();
            }
        }

        // TPSカメラをアクティブにする
        public void ActivateTpsCamera(bool activate)
        {
            m_IsTpsActive = activate && m_UseTpsCamera;
            
            if (m_IsTpsActive)
            {
                m_Camera.orthographic = false;
                m_Camera.fieldOfView = m_TpsFieldOfView;
                // TPS起動時に即座に位置と回転を設定
                Transform myTank = GetMyTank();
                if (myTank != null)
                {
                    m_LastTargetForward = myTank.forward;
                    transform.position = myTank.position 
                        - m_LastTargetForward * m_TpsDistance // タンクの後ろ（安定した向きを使用）
                        + Vector3.up * m_TpsHeight // 高さオフセット
                        + Vector3.Cross(Vector3.up, m_LastTargetForward).normalized * m_TpsOffsetX; // 安定した横オフセット
                    m_TpsVelocity = Vector3.zero;
                    // プレイヤーを注視する
                    Vector3 lookDir = myTank.position - transform.position;
                    if (lookDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(lookDir);
                    }
                    
                    // 障害物透明化の設定
                    if (m_ObstacleHandler != null && m_UseObstacleTransparency)
                    {
                        m_ObstacleHandler.SetTarget(myTank, true);
                    }
                }
            }
            else
            {
                m_Camera.orthographic = true;
                // 視野角を元に戻す
                m_Camera.fieldOfView = m_OriginalFieldOfView;
                SetStartPositionAndSize();
                
                // 障害物透明化を無効化
                if (m_ObstacleHandler != null)
                {
                    m_ObstacleHandler.SetTarget(null, false);
                }
            }
        }

        // TPSカメラの移動処理
        private void MoveTps()
        {
            Transform myTank = GetMyTank();
            if (myTank == null)
                return;
            
            // プレイヤーの向きが大きく変わった場合のみ更新（安定性向上）
            if (Vector3.Angle(myTank.forward, m_LastTargetForward) > 5f)
            {
                m_LastTargetForward = myTank.forward;
            }
            
            // プレイヤーの向きに合わせて後ろに配置
            m_TpsDesiredPosition = myTank.position 
                - m_LastTargetForward * m_TpsDistance // タンクの後ろ（安定した向きを使用）
                + Vector3.up * m_TpsHeight // 高さオフセット
                + Vector3.Cross(Vector3.up, m_LastTargetForward).normalized * m_TpsOffsetX; // 安定した横オフセット
            
            transform.position = Vector3.SmoothDamp(transform.position, m_TpsDesiredPosition, ref m_TpsVelocity, m_TpsDampTime);
            
            // プレイヤーを注視する
            Vector3 lookDirection = myTank.position - transform.position;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_TpsDampTime * 5f);
            }
        }

        /// <summary>
        /// 自分のタンクのTransformを取得
        /// </summary>
        private Transform GetMyTank()
        {
            if (m_Targets == null || m_Targets.Length == 0)
                return null;

            // GameManagerを探してネットワークモードかどうか確認
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null && gameManager.m_UseNetworkMode)
            {
                // ネットワークモードの場合、LocalInputProviderを持つタンクを探す
                for (int i = 0; i < gameManager.m_Tanks.Length; i++)
                {
                    var tank = gameManager.m_Tanks[i];
                    if (tank.m_Instance != null && tank.InputProvider is LocalInputProvider)
                    {
                        return tank.m_Instance.transform;
                    }
                }
            }
            
            // ローカルモードまたは見つからない場合は最初のターゲットを返す
            return m_Targets.Length > 0 ? m_Targets[0] : null;
        }


        private void Move ()
        {
            // Find the average position of the targets.
            FindAveragePosition ();

            // Smoothly transition to that position.
            transform.position = Vector3.SmoothDamp(transform.position, m_DesiredPosition, ref m_MoveVelocity, m_DampTime);
            
            // 俯瞰視点の場合、元の回転に戻す
            if (!m_IsTpsActive)
            {
                transform.rotation = m_OriginalRotation;
            }
        }


        private void FindAveragePosition ()
        {
            Vector3 averagePos = new Vector3 ();
            int numTargets = 0;

            // Go through all the targets and add their positions together.
            for (int i = 0; i < m_Targets.Length; i++)
            {
                // If the target isn't active, go on to the next one.
                if (!m_Targets[i].gameObject.activeSelf)
                    continue;

                // Add to the average and increment the number of targets in the average.
                averagePos += m_Targets[i].position;
                numTargets++;
            }

            // If there are targets divide the sum of the positions by the number of them to find the average.
            if (numTargets > 0)
                averagePos /= numTargets;

            // Keep the same y value.
            averagePos.y = transform.position.y;

            // The desired position is the average position;
            m_DesiredPosition = averagePos;
        }


        private void Zoom ()
        {
            // Find the required size based on the desired position and smoothly transition to that size.
            float requiredSize = FindRequiredSize();
            m_Camera.orthographicSize = Mathf.SmoothDamp (m_Camera.orthographicSize, requiredSize, ref m_ZoomSpeed, m_DampTime);
        }


        private float FindRequiredSize ()
        {
            // Find the position the camera rig is moving towards in its local space.
            Vector3 desiredLocalPos = transform.InverseTransformPoint(m_DesiredPosition);

            // Start the camera's size calculation at zero.
            float size = 0f;

            // Go through all the targets...
            for (int i = 0; i < m_Targets.Length; i++)
            {
                // ... and if they aren't active continue on to the next target.
                if (!m_Targets[i].gameObject.activeSelf)
                    continue;

                // Otherwise, find the position of the target in the camera's local space.
                Vector3 targetLocalPos = transform.InverseTransformPoint(m_Targets[i].position);

                // Find the position of the target from the desired position of the camera's local space.
                Vector3 desiredPosToTarget = targetLocalPos - desiredLocalPos;

                // Choose the largest out of the current size and the distance of the tank 'up' or 'down' from the camera.
                size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.y));

                // Choose the largest out of the current size and the calculated size based on the tank being to the left or right of the camera.
                size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.x) / m_Camera.aspect);
            }

            // Add the edge buffer to the size.
            size += m_ScreenEdgeBuffer;

            // Make sure the camera's size isn't below the minimum.
            size = Mathf.Max (size, m_MinSize);

            return size;
        }


        public void SetStartPositionAndSize ()
        {
            // 俯瞰視点に戻す（元の回転を復元）
            m_IsTpsActive = false;
            m_Camera.orthographic = true;
            transform.rotation = m_OriginalRotation;
            
            // Find the desired position.
            FindAveragePosition ();

            // Set the camera's position to the desired position without damping.
            transform.position = m_DesiredPosition;

            // Find and set the required size of the camera.
            m_Camera.orthographicSize = FindRequiredSize ();
        }
    }
}