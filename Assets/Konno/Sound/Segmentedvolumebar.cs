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
    [Tooltip("ONにすると、境目ではなく「現在レベルのブロックの真ん中」を指す(レベル0は1個目のブロックの真ん中)")]
    [SerializeField] private bool pointAtSegmentCenter = false;
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
    /// levelMarkerを、現在の音量レベルの位置へ移動する。
    ///
    /// 以前は「バー全体のRectTransformの横幅 × レベル/最大」で位置を決めていたため、
    /// バーの枠とブロックの実際の並び(余白・ブロック間の隙間・Layout Groupの余白)が
    /// 一致していないと、ブロックの境目からずれていた。
    /// 今回は、実際のブロック(segments)の端の位置を直接読み取って合わせる。
    ///   ・レベル0      : 1個目のブロックの左端
    ///   ・レベル1以上  : 赤く塗られた最後のブロックの右端(=赤とグレーの境目)
    /// 高さは、そのブロックの上端。
    ///
    /// マーカーのPivotが何であっても、「マーカーの下辺の中央」が目的の点に来るように補正する。
    /// </summary>
    private void UpdateMarkerPosition()
    {
        if (levelMarker == null) return;

        Vector3 point;
        if (!TryGetSegmentPoint(out point))
        {
            // ブロックが未設定の時だけ、従来どおりバー全体の割合で位置を決める
            RectTransform barRect = transform as RectTransform;
            if (barRect == null) return;

            Vector3[] corners = new Vector3[4];
            barRect.GetWorldCorners(corners);
            float ratio = MaxLevel > 0 ? (float)currentLevel / MaxLevel : 0f;
            point = Vector3.Lerp(corners[1], corners[2], ratio);
        }

        // 目的の点から少し上へ(Canvasのスケールを考慮して変換)
        RectTransform markerParent = levelMarker.parent as RectTransform;
        Vector3 offset = markerParent != null
            ? markerParent.TransformVector(new Vector3(0f, markerYOffset, 0f))
            : new Vector3(0f, markerYOffset, 0f);
        Vector3 target = point + offset;

        // マーカーの「下辺の中央」を target に合わせる。
        // position はPivotの位置を指すので、Pivot→下辺中央 のベクトルぶんだけ補正する
        Rect r = levelMarker.rect;
        Vector3 pivotToBottomCenterLocal = new Vector3(
            (0.5f - levelMarker.pivot.x) * r.width,
            (0f - levelMarker.pivot.y) * r.height,
            0f);
        Vector3 pivotToBottomCenterWorld = levelMarker.TransformVector(pivotToBottomCenterLocal);

        levelMarker.position = target - pivotToBottomCenterWorld;
    }

    /// <summary>
    /// 現在のレベルが指すブロックの端(ワールド座標)を返す。
    /// X: レベル0なら1個目の左端、1以上ならレベル番目のブロックの右端。 Y: そのブロックの上端。
    /// </summary>
    private bool TryGetSegmentPoint(out Vector3 point)
    {
        point = Vector3.zero;
        if (segments == null || segments.Length == 0) return false;

        int index = Mathf.Clamp(currentLevel - 1, 0, segments.Length - 1);
        Image seg = segments[index];
        if (seg == null) return false;

        Vector3[] c = new Vector3[4];
        seg.rectTransform.GetWorldCorners(c); // 0:左下 1:左上 2:右上 3:右下

        if (pointAtSegmentCenter)
        {
            point = (c[1] + c[2]) * 0.5f;
        }
        else
        {
            point = currentLevel <= 0 ? c[1] : c[2];
        }
        return true;
    }

    private void UpdateMarkerVisibility()
    {
        if (levelMarker == null) return;

        bool visible = !showMarkerOnlyWhenFocused || isFocused;
        levelMarker.gameObject.SetActive(visible);
    }
}