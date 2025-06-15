using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Complete
{
    /// <summary>
    /// カメラとプレイヤーの間の障害物を透明化するコンポーネント
    /// 単一責任原則に従い、障害物の透明化のみを担当
    /// </summary>
    public class CameraObstacleHandler : MonoBehaviour
    {
        [Header("Obstacle Transparency Settings")]
        [Tooltip("障害物の透明化を有効にするかどうか")]
        public bool m_EnableTransparency = true;         // 障害物の透明化を有効にするかどうか
        
        [Tooltip("障害物の透明度 (0=完全透明, 1=不透明)")]
        [Range(0.0f, 1.0f)]
        public float m_TransparencyAmount = 0.3f;        // 障害物の透明度
        
        [Tooltip("透明化の対象となるレイヤー (これらのレイヤーにあるオブジェクトが透明化されます)")]
        public LayerMask m_ObstacleLayers;               // 透明化の対象となるレイヤー
        
        public float m_CheckInterval = 0.1f;             // 障害物チェックの間隔（秒）
        
        [Header("Debug Settings")]
        [Tooltip("デバッグ用にレイを可視化するかどうか")]
        public bool m_VisualizeRays = false;             // レイを可視化
        [Tooltip("デバッグレイの色")]
        public Color m_RayColor = Color.red;             // レイの色
        [Tooltip("デバッグレイの持続時間 (秒)")]
        public float m_RayDuration = 0.0f;               // レイの持続時間（0で1フレームのみ表示）
        [Tooltip("デバッグ情報をコンソールに出力するかどうか")]
        public bool m_LogDebugInfo = false;              // デバッグ情報をログに出力
        [Tooltip("シーンビューにギズモを描画するかどうか")]
        public bool m_DrawGizmos = true;                 // ギズモを描画

        private Dictionary<Renderer, Material[]> m_OriginalMaterials = new Dictionary<Renderer, Material[]>();
        private List<Renderer> m_CurrentTransparentObjects = new List<Renderer>();
        private Camera m_Camera;
        private Transform m_Target;
        private CancellationTokenSource m_CancellationTokenSource;
        private Dictionary<Renderer, Material[]> m_TransparentMaterials = new Dictionary<Renderer, Material[]>();

        private void Awake()
        {
            // カメラコンポーネントを取得
            m_Camera = GetComponentInChildren<Camera>();
            
            // カメラが見つからない場合は警告
            if (m_Camera == null)
            {
                Debug.LogWarning("CameraObstacleHandler: カメラコンポーネントが見つかりません。同じGameObjectまたは子オブジェクトにCameraコンポーネントがあることを確認してください。");
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            // CancellationTokenSource のキャンセルと破棄
            try
            {
                m_CancellationTokenSource?.Cancel();
            }
            catch (System.Exception ex)
            {
                if (m_LogDebugInfo)
                {
                    Debug.LogError($"CancellationTokenSource キャンセル中のエラー: {ex.Message}");
                }
            }
            finally
            {
                m_CancellationTokenSource?.Dispose();
                m_CancellationTokenSource = null;
            }

            RestoreAllMaterials();
            
            // 透明マテリアルのクリーンアップ
            foreach (var kvp in m_TransparentMaterials)
            {
                foreach (Material mat in kvp.Value)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
            }
            m_TransparentMaterials.Clear();
        }

        /// <summary>
        /// ターゲットを設定（アクティブ化時に呼び出す）
        /// </summary>
        /// <param name="target">プレイヤーのTransform</param>
        /// <param name="active">透明化機能をアクティブにするかどうか</param>
        public void SetTarget(Transform target, bool active)
        {
            m_Target = target;
            m_EnableTransparency = active;

            if (!active)
            {
                RestoreAllMaterials();
            }

            // UniTask を使った非同期障害物チェック開始/停止
            if (active)
            {
                m_CancellationTokenSource?.Cancel();
                m_CancellationTokenSource = new CancellationTokenSource();
                _ = ObstacleCheckLoopAsync(m_CancellationTokenSource.Token);
            }
            else
            {
                m_CancellationTokenSource?.Cancel();
                m_CancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 障害物の透明化処理
        /// </summary>
        private void HandleObstacleTransparency()
        {
            // 既存の透明オブジェクトを追跡して元に戻すべきものを判断
            List<Renderer> objectsToRestore = new List<Renderer>(m_CurrentTransparentObjects);
            
            if (m_Target == null)
            {
                RestoreAllMaterials();
                return;
            }

            // カメラのNear面の位置を計算
            Vector3 rayOrigin = m_Camera.transform.position + m_Camera.transform.forward * m_Camera.nearClipPlane;
            Vector3 direction = m_Target.position - rayOrigin;
            float distance = direction.magnitude;
            
            // デバッグ用にレイを可視化
            if (m_VisualizeRays)
            {
                // カメラのNear面を示す小さな円を描画
                Debug.DrawRay(rayOrigin, Vector3.up * 0.1f, Color.yellow, m_RayDuration);
                Debug.DrawRay(rayOrigin, Vector3.right * 0.1f, Color.yellow, m_RayDuration);
                
                // Near面からターゲットへのレイを描画
                Debug.DrawRay(rayOrigin, direction.normalized * distance, m_RayColor, m_RayDuration);
            }

            // カメラのNear面からプレイヤーへのレイキャスト
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, direction.normalized, distance, m_ObstacleLayers);

            foreach (RaycastHit hit in hits)
            {
                // デバッグ用にヒットポイントを可視化
                if (m_VisualizeRays)
                {
                    // ヒットポイントに小さな球を描画
                    Debug.DrawLine(hit.point, hit.point + Vector3.up * 0.2f, Color.green, m_RayDuration);
                    Debug.DrawLine(hit.point, hit.point + Vector3.right * 0.2f, Color.green, m_RayDuration);
                    Debug.DrawLine(hit.point, hit.point + Vector3.forward * 0.2f, Color.green, m_RayDuration);
                }
                
                // プレイヤー自身は除外
                if (hit.transform == m_Target || hit.transform.IsChildOf(m_Target))
                    continue;

                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // デバッグログ出力
                    if (m_LogDebugInfo)
                    {
                        Debug.Log($"障害物検出: {hit.transform.name}, 距離: {hit.distance:F2}m, レイヤー: {LayerMask.LayerToName(hit.transform.gameObject.layer)}");
                    }
                    
                    // この障害物は引き続き透明なので、復元リストから除外
                    objectsToRestore.Remove(renderer);
                    
                    // まだ透明になっていない場合のみ処理
                    if (!m_CurrentTransparentObjects.Contains(renderer))
                    {
                        MakeTransparent(renderer);
                        m_CurrentTransparentObjects.Add(renderer);
                    }
                }
            }

            // レイに当たらなくなったオブジェクトを元に戻す
            foreach (Renderer renderer in objectsToRestore)
            {
                RestoreMaterial(renderer);
                m_CurrentTransparentObjects.Remove(renderer);
            }
        }

        /// <summary>
        /// オブジェクトを透明化する
        /// </summary>
        private void MakeTransparent(Renderer renderer)
        {
            // 元のマテリアルを保存（まだ保存されていない場合のみ）
            if (!m_OriginalMaterials.ContainsKey(renderer))
            {
                // 配列の参照ではなくディープコピーを作成
                Material[] originalMats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    originalMats[i] = renderer.sharedMaterials[i];
                }
                m_OriginalMaterials[renderer] = originalMats;
            }

            // 透明マテリアルを作成
            Material[] transparentMaterials = new Material[renderer.sharedMaterials.Length];
            
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                // 既存のマテリアルをコピー
                transparentMaterials[i] = new Material(renderer.sharedMaterials[i]);
                
                // URPのみの透明化設定
                ApplyURPTransparency(transparentMaterials[i], renderer.name);
            }

            // 透明マテリアルをキャッシュ
            m_TransparentMaterials[renderer] = transparentMaterials;
            
            // 新しいマテリアルを適用（sharedMaterialsではなくmaterialsを使用）
            renderer.materials = transparentMaterials;
            
            // デバッグログ
            if (m_LogDebugInfo)
            {
                Debug.Log($"URP透明化適用: {renderer.name}, マテリアル数: {transparentMaterials.Length}");
            }
        }
        
        // URP用の透明化処理
        private void ApplyURPTransparency(Material material, string objectName)
        {
            try
            {
                // URP用のサーフェスタイプとブレンドモードを設定
                if (material.HasProperty("_Surface"))
                {
                    material.SetFloat("_Surface", 1); // 1 = Transparent
                }
                
                // URP用のアルファクリップを無効化
                if (material.HasProperty("_AlphaClip"))
                {
                    material.SetFloat("_AlphaClip", 0); // 0 = Disabled
                }
                
                // URP用のブレンドモード設定
                if (material.HasProperty("_Blend"))
                {
                    material.SetFloat("_Blend", 0); // 0 = SrcAlpha OneMinusSrcAlpha
                }
                
                // URP用のSrcBlendとDstBlend
                if (material.HasProperty("_SrcBlend"))
                {
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                }
                
                if (material.HasProperty("_DstBlend"))
                {
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }
                
                // URP用のZWrite
                if (material.HasProperty("_ZWrite"))
                {
                    material.SetFloat("_ZWrite", 0); // 0 = Off
                }
                
                // URP用のキーワード設定
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                
                // URP用のレンダーキュー
                material.renderQueue = 3000; // Transparent
                
                // URP用のベースカラーのアルファ値設定
                if (material.HasProperty("_BaseColor"))
                {
                    Color baseColor = material.GetColor("_BaseColor");
                    baseColor.a = m_TransparencyAmount;
                    material.SetColor("_BaseColor", baseColor);
                    
                    // デバッグログ
                    if (m_LogDebugInfo)
                    {
                        Debug.Log($"URP BaseColor設定: {objectName}, Alpha: {m_TransparencyAmount}");
                    }
                }
                
                // _Colorプロパティもある場合は設定
                if (material.HasProperty("_Color"))
                {
                    Color color = material.GetColor("_Color");
                    color.a = m_TransparencyAmount;
                    material.SetColor("_Color", color);
                }
                
                // Shader.SetGlobalFloatを使って透明度を設定（一部のURPシェーダーで必要）
                Shader.SetGlobalFloat("_GlobalTransparency", m_TransparencyAmount);
                
                // レンダリングタイプを明示的に設定
                material.SetOverrideTag("RenderType", "Transparent");
                
                // デバッグログ
                if (m_LogDebugInfo)
                {
                    Debug.Log($"URP透明化適用: {objectName}");
                }
            }
            catch (System.Exception e)
            {
                if (m_LogDebugInfo)
                {
                    Debug.LogWarning($"URP透明化設定に失敗: {objectName} - {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// 特定のオブジェクトのマテリアルを元に戻す
        /// </summary>
        private void RestoreMaterial(Renderer renderer)
        {
            if (renderer != null)
            {
                // 通常のマテリアル復元処理
                if (m_OriginalMaterials.ContainsKey(renderer))
                {
                    try
                    {
                        // 元のマテリアルを復元
                        renderer.materials = m_OriginalMaterials[renderer];
                        
                        // デバッグログ
                        if (m_LogDebugInfo)
                        {
                            Debug.Log($"マテリアル復元: {renderer.name}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        if (m_LogDebugInfo)
                        {
                            Debug.LogError($"マテリアル復元エラー: {renderer.name} - {e.Message}");
                        }
                    }
                    
                    // 透明マテリアルのキャッシュからも削除
                    if (m_TransparentMaterials.ContainsKey(renderer))
                    {
                        // キャッシュされた透明マテリアルを破棄
                        foreach (Material mat in m_TransparentMaterials[renderer])
                        {
                            if (Application.isPlaying)
                            {
                                Destroy(mat);
                            }
                        }
                        m_TransparentMaterials.Remove(renderer);
                    }
                }
            }
        }

        /// <summary>
        /// すべてのマテリアルを元に戻す
        /// </summary>
        private void RestoreAllMaterials()
        {
            foreach (Renderer renderer in m_CurrentTransparentObjects)
            {
                RestoreMaterial(renderer);
            }
            
            m_CurrentTransparentObjects.Clear();
        }
        
        /// <summary>
        /// シーンビューにギズモを描画
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!m_DrawGizmos || !Application.isPlaying || m_Target == null || !m_EnableTransparency)
                return;
                
            // カメラ位置にワイヤーフレームの球を描画
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // カメラのNear面の位置を計算して表示
            if (m_Camera != null)
            {
                Vector3 nearPlaneCenter = m_Camera.transform.position + m_Camera.transform.forward * m_Camera.nearClipPlane;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(nearPlaneCenter, 0.2f);
                
                // Near面からターゲットへの線を描画
                Gizmos.color = m_RayColor;
                Gizmos.DrawLine(nearPlaneCenter, m_Target.position);
            }
            else
            {
                // カメラが見つからない場合は通常のライン
                Gizmos.color = m_RayColor;
                Gizmos.DrawLine(transform.position, m_Target.position);
            }
            
            // ターゲット位置に球を描画
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(m_Target.position, 0.5f);
            
            // 現在透明化されているオブジェクトを表示
            Gizmos.color = new Color(0, 1, 1, 0.5f); // 水色
            foreach (Renderer renderer in m_CurrentTransparentObjects)
            {
                if (renderer != null)
                {
                    // バウンディングボックスを描画
                    Bounds bounds = renderer.bounds;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
        }

        // UniTaskを使った非同期障害物チェック
        private async UniTaskVoid ObstacleCheckLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    HandleObstacleTransparency();
                    await UniTask.Delay((int)(m_CheckInterval * 1000), cancellationToken: token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // キャンセル時は何もしない
            }
        }
    }
} 