using UnityEngine;

/// <summary>
/// 2人のプレイヤーの中間を追従し、
/// 距離に応じてカメラをズームする。
/// </summary>
public sealed class FightingCameraController : MonoBehaviour
{
    [Header("追従対象")]
    [SerializeField]
    private Transform player1;

    [SerializeField]
    private Transform player2;


    [Header("カメラ位置")]
    [SerializeField]
    private float cameraHeight = 1.5f;

    [SerializeField]
    private float cameraZ = -10f;

    [SerializeField, Min(0f)]
    private float followSmooth = 8f;


    [Header("ズーム")]
    [SerializeField]
    private Camera targetCamera;

    [SerializeField, Min(0.1f)]
    private float minOrthographicSize = 5f;

    [SerializeField, Min(0.1f)]
    private float maxOrthographicSize = 8f;

    [SerializeField, Min(0.1f)]
    private float minPlayerDistance = 3f;

    [SerializeField, Min(0.1f)]
    private float maxPlayerDistance = 10f;

    [SerializeField, Min(0f)]
    private float zoomSmooth = 5f;


    [Header("カメラ移動制限")]
    [SerializeField]
    private bool useCameraLimit = false;

    [SerializeField]
    private float minX = -10f;

    [SerializeField]
    private float maxX = 10f;


    private void Reset()
    {
        targetCamera =
            GetComponent<Camera>();
    }


    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera =
                GetComponent<Camera>();
        }
    }


    private void LateUpdate()
    {
        if (player1 == null ||
            player2 == null ||
            targetCamera == null)
        {
            return;
        }

        // 先にズームを計算
        UpdateCameraZoom();

        // そのズーム量を使ってカメラ位置を制限
        UpdateCameraPosition();
    }



    /// <summary>
    /// 2人の中間を追いながら、
    /// ステージ外が映らないようにX位置を制限する。
    /// </summary>
    private void UpdateCameraPosition()
    {
        Vector3 middle =
            (player1.position +
             player2.position) * 0.5f;

        float targetX =
            middle.x;


        // =========================
        // ステージ端制限
        // =========================

        if (useCameraLimit)
        {
            // Orthographic Cameraの画面半分の横幅
            float halfCameraWidth =
                targetCamera.orthographicSize *
                targetCamera.aspect;

            // カメラの左端・右端が
            // ステージ外に出ない位置
            float cameraMinX =
                minX + halfCameraWidth;

            float cameraMaxX =
                maxX - halfCameraWidth;


            // ステージよりカメラ画面の方が広い場合
            if (cameraMinX > cameraMaxX)
            {
                targetX =
                    (minX + maxX) * 0.5f;
            }
            else
            {
                targetX =
                    Mathf.Clamp(
                        targetX,
                        cameraMinX,
                        cameraMaxX
                    );
            }
        }


        Vector3 targetPosition =
            new Vector3(
                targetX,
                cameraHeight,
                cameraZ
            );


        transform.position =
            Vector3.Lerp(
                transform.position,
                targetPosition,
                followSmooth *
                Time.deltaTime
            );
    }



    /// <summary>
    /// 2人の距離に応じてズームする。
    /// </summary>
    private void UpdateCameraZoom()
    {
        float distance =
            Mathf.Abs(
                player1.position.x -
                player2.position.x
            );

        float t =
            Mathf.InverseLerp(
                minPlayerDistance,
                maxPlayerDistance,
                distance
            );

        float targetSize =
            Mathf.Lerp(
                minOrthographicSize,
                maxOrthographicSize,
                t
            );

        targetCamera.orthographicSize =
            Mathf.Lerp(
                targetCamera.orthographicSize,
                targetSize,
                zoomSmooth *
                Time.deltaTime
            );
    }
    private void OnDrawGizmosSelected()
    {
        if (!useCameraLimit)
        {
            return;
        }

        Gizmos.DrawLine(
            new Vector3(minX, -10f, 0f),
            new Vector3(minX, 10f, 0f)
        );

        Gizmos.DrawLine(
            new Vector3(maxX, -10f, 0f),
            new Vector3(maxX, 10f, 0f)
        );
    }
}
