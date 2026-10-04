using UnityEngine;

/// <summary>
/// プレイヤーキャラクターの頭上に、「どちらがP1/P2か」を示す逆三角形などの
/// UI画像を表示し、キャラクターの動きに追従させる汎用スクリプト。
///
/// P1・P2の両方を1つのスクリプトで管理できるように、Inspectorで
/// プレイヤーごとの設定(追従対象のTransformと、表示する画像のRectTransform)を
/// 配列で指定する方式にしている。画像自体はP1/P2で同じものを共通利用しても、
/// 別々の画像(色違いなど)を使ってもどちらでも対応できる。
///
/// 前提:
///   ・表示する逆三角形はCanvas上のUI Image(RectTransformを持つもの)
///   ・そのCanvasのRender ModeはScreen Space - Overlay / Screen Space - Camera の
///     どちらでも動作する(World Spaceの場合は別途調整が必要)
///
/// 使い方:
///   1. 空のGameObjectにこのスクリプトをアタッチ(シーンに1つだけでよい)
///   2. Canvasフィールドに、逆三角形画像が置かれているCanvasを割り当てる
///   3. Playersの要素0をP1用、要素1をP2用として、
///        Target  : P1/P2キャラクターのTransform(キャラ本体でよい)
///        Icon    : 頭上に出す逆三角形画像のRectTransform
///        World Offset : キャラの基準点から見た頭上オフセット(通常Yのみでよい)
///      を設定する
///   4. キャラクターが実行中に入れ替わる場合は、SetTarget()で
///      追従対象を後から変更できる
/// </summary>
public class PlayerAboveHeadIndicator : MonoBehaviour
{
    [System.Serializable]
    public class IndicatorEntry
    {
        [Tooltip("Inspector上で分かりやすくするためのラベル(P1 / P2など)。" +
                 "処理には使用しない")]
        public string label;

        [Tooltip("追従対象(P1またはP2のキャラクターのTransform)")]
        public Transform target;

        [Tooltip("頭上に表示する逆三角形画像のRectTransform")]
        public RectTransform icon;

        [Tooltip("targetの位置から見た、頭上に表示したいオフセット(ワールド座標の単位)。" +
                 "通常はYだけプラスにして頭の上に浮かせる")]
        public Vector3 worldOffset = new Vector3(0f, 2f, 0f);

        [HideInInspector] public bool visible = true;
    }

    [Header("参照")]
    [Tooltip("ワールド座標→スクリーン座標の変換に使うカメラ。未設定ならCamera.mainを使う")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("逆三角形画像が置かれているCanvas。" +
             "Screen Space - Overlay / Screen Space - Camera のどちらでも動作する")]
    [SerializeField] private Canvas canvas;

    [Header("プレイヤーごとの設定")]
    [Tooltip("要素0=P1、要素1=P2、という想定。3人以上に増やしても動作する")]
    [SerializeField] private IndicatorEntry[] players = new IndicatorEntry[2];

    private void LateUpdate()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || canvas == null) return;

        for (int i = 0; i < players.Length; i++)
        {
            IndicatorEntry entry = players[i];
            if (entry == null || entry.target == null || entry.icon == null)
                continue;

            if (!entry.visible)
            {
                if (entry.icon.gameObject.activeSelf) entry.icon.gameObject.SetActive(false);
                continue;
            }

            if (!entry.icon.gameObject.activeSelf) entry.icon.gameObject.SetActive(true);

            Vector3 worldPos = entry.target.position + entry.worldOffset;
            Vector2 screenPoint = cam.WorldToScreenPoint(worldPos);

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                entry.icon.position = screenPoint;
            }
            else
            {
                Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null
                    ? canvas.worldCamera
                    : cam;

                RectTransform canvasRect = canvas.transform as RectTransform;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        canvasRect, screenPoint, eventCamera, out Vector3 worldPoint))
                {
                    entry.icon.position = worldPoint;
                }
            }
        }
    }

    /// <summary>
    /// 指定したプレイヤー番号(0=P1, 1=P2)の追従対象を後から変更する。
    /// キャラクター選択結果に応じて、生成されたキャラのTransformを割り当てる用途など。
    /// </summary>
    public void SetTarget(int playerIndex, Transform newTarget)
    {
        if (playerIndex < 0 || playerIndex >= players.Length) return;
        players[playerIndex].target = newTarget;
    }

    /// <summary>
    /// 指定したプレイヤー番号のインジケーターの表示/非表示を切り替える。
    /// </summary>
    public void SetVisible(int playerIndex, bool visible)
    {
        if (playerIndex < 0 || playerIndex >= players.Length) return;
        players[playerIndex].visible = visible;
    }
}
