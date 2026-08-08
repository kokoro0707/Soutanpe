using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 10個のブロックを並べて音量レベル(0から10)を表現するゲージ。
/// 見た目だけを担当し、音量計算やAudioManagerとの連携は持たない
/// (VolumeChannelRow側から SetLevel() を呼んでもらう)。
///
/// Hierarchy例:
///   VolumeBar
///    ├─ Segment0
///    ├─ Segment1
///    ├─ ...
///    └─ Segment9
///   (10個のImageを横に並べて、この配列に登録する)
///
/// LevelMarker(▽の三角マーカー)はVolumeBarの外(例: 同じRow直下)に置いてもよい。
/// ワールド座標(RectTransform.position)を直接指定する方式にしているため、
/// 親のPivot設定に関係なく正しい位置に配置される。
/// </summary>
public class SegmentedVolumeBar : MonoBehaviour
{
    [Header("ブロック(左から順に10個)")]
    [SerializeField] private Image[] segments = new Image[10];

    [Header("色")]
    [SerializeField] private Color filledColor = new Color(0.85f, 0.15f, 0.15f); // 赤
    [SerializeField] private Color emptyColor = new Color(0.55f, 0.55f, 0.55f);  // グレー

    [Header("現在位置マーカー(▽の三角)")]
    [Tooltip("現在の音量レベルの位置を指す小さな三角マーカー(RectTransform)。サイズは固定のまま、位置だけ自動で動く")]
    [SerializeField] private RectTransform levelMarker;
    [Tooltip("バー上端からのオフセット(ワールド単位に近い見た目にするため、実際はCanvasのスケールに応じて自動調整される)")]
    [SerializeField] private float markerYOffset = 4f;
    [Tooltip("ONの場合、SetFocused(true)のときだけマーカーを表示する。OFFなら常に表示")]
    [SerializeField] private bool showMarkerOnlyWhenFocused = true;

    private int currentLevel;
    private bool isFocused;

    public int MaxLevel => segments.Length;
    public int CurrentLevel => currentLevel;

    private void LateUpdate()
    {
        // Horizontal Layout Group等によるサイズ確定タイミングに関係なく、
        // 毎フレーム位置を合わせ直す(静的なUIなので負荷はごくわずか)
        UpdateMarkerPosition();
    }

    /// <summary>
    /// 音量レベル(0から10)を反映する。
    /// </summary>
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 0, segments.Length);

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null) continue;
            segments[i].color = i < currentLevel ? filledColor : emptyColor;
        }

        UpdateMarkerPosition();
    }

    /// <summary>
    /// このバーが現在キー操作の対象として選択されているかどうかを切り替える。
    /// </summary>
    public void SetFocused(bool focused)
    {
        isFocused = focused;
        UpdateMarkerVisibility();
    }

    /// <summary>
    /// levelMarkerを、現在の音量レベルに応じたバー上端の位置(ワールド座標)へ移動する。
    /// RectTransform.position(ワールド座標)を直接指定するため、
    /// levelMarkerの親が誰であっても、Pivotが何であっても正しい位置になる。
    /// </summary>
    private void UpdateMarkerPosition()
    {
        if (levelMarker == null) return;

        RectTransform barRect = transform as RectTransform;
        if (barRect == null) return;

        // バー自身のワールド座標での四隅を取得(0:左下 1:左上 2:右上 3:右下)
        Vector3[] corners = new Vector3[4];
        barRect.GetWorldCorners(corners);

        float ratio = MaxLevel > 0 ? (float)currentLevel / MaxLevel : 0f;

        // 左上から右上を、現在レベルの割合で補間 → バー上端の該当位置(ワールド座標)
        Vector3 topEdgePoint = Vector3.Lerp(corners[1], corners[2], ratio);

        // Y方向に少しだけ上へオフセット(Canvasのスケールを考慮して変換)
        RectTransform markerParent = levelMarker.parent as RectTransform;
        Vector3 offset = markerParent != null
            ? markerParent.TransformVector(new Vector3(0f, markerYOffset, 0f))
            : new Vector3(0f, markerYOffset, 0f);

        levelMarker.position = topEdgePoint + offset;
    }

    private void UpdateMarkerVisibility()
    {
        if (levelMarker == null) return;

        bool visible = !showMarkerOnlyWhenFocused || isFocused;
        levelMarker.gameObject.SetActive(visible);
    }
}