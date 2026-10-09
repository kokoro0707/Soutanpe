using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 斜めの境界線がグネグネ波打つ塗りつぶしグラフィック(UI)。
/// RectTransform の中で、左端の高さ→右端の高さを結ぶ斜め線を境界にして、
/// その上側(または下側)を Color で塗る。境界線は常に波打つ。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class WavyEdgeGraphic : MaskableGraphic
{
    public enum FillSide { Above, Below }

    [Header("境界線の位置(0=下端 1=上端)")]
    [SerializeField] private FillSide fillSide = FillSide.Above;
    [SerializeField, Range(-0.5f, 1.5f)] private float leftEdge = 0.12f;
    [SerializeField, Range(-0.5f, 1.5f)] private float rightEdge = 0.92f;
    [SerializeField, Range(16, 512)] private int segments = 180;

    [Header("波1(大きなうねり)")]
    [SerializeField] private float amplitude1 = 14f;
    [SerializeField] private float wavelength1 = 260f;
    [SerializeField] private float speed1 = 0.6f;

    [Header("波2(細かいグネグネ)")]
    [SerializeField] private float amplitude2 = 6f;
    [SerializeField] private float wavelength2 = 95f;
    [SerializeField] private float speed2 = -1.3f;

    [Header("波3(ゆらぎ)")]
    [SerializeField] private float amplitude3 = 3f;
    [SerializeField] private float wavelength3 = 41f;
    [SerializeField] private float speed3 = 2.1f;

    [Tooltip("ONで斜め線に対して垂直方向に波打つ。OFFで真上下に波打つ")]
    [SerializeField] private bool perpendicular = true;
    [Tooltip("両端で波を0にするフェード幅(px)。0で無効")]
    [SerializeField] private float edgeFade = 0f;

    [Header("境界ライン(任意)")]
    [SerializeField] private bool drawEdgeLine = true;
    [SerializeField] private Color edgeLineColor = new Color(0.55f, 0.85f, 1f, 0.8f);
    [SerializeField] private float edgeLineWidth = 2f;

    [Header("アニメーション")]
    [SerializeField] private bool animate = true;
    [SerializeField] private bool useUnscaledTime = true;

    private float time;

    private void Update()
    {
        if (!animate) return;
        time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0f || r.height <= 0f) return;

        int n = Mathf.Max(2, segments);
        var pts = new Vector2[n + 1];

        Vector2 a = new Vector2(r.xMin, r.yMin + leftEdge * r.height);
        Vector2 b = new Vector2(r.xMax, r.yMin + rightEdge * r.height);
        Vector2 dirLine = (b - a);
        float len = dirLine.magnitude;
        dirLine /= Mathf.Max(0.0001f, len);
        Vector2 normal = new Vector2(-dirLine.y, dirLine.x); // 上側を向く法線

        for (int i = 0; i <= n; i++)
        {
            float u = (float)i / n;
            Vector2 p = Vector2.Lerp(a, b, u);
            float s = u * len;
            float w = Wave(s);

            if (edgeFade > 0f)
            {
                float f = Mathf.Clamp01(Mathf.Min(s, len - s) / edgeFade);
                w *= f;
            }

            p += perpendicular ? normal * w : new Vector2(0f, w);
            pts[i] = p;
        }

        // 塗り(境界線 → 上端 or 下端)
        Color32 col = color;
        float capY = fillSide == FillSide.Above ? r.yMax : r.yMin;
        for (int i = 0; i <= n; i++)
        {
            vh.AddVert(pts[i], col, Vector2.zero);
            vh.AddVert(new Vector2(pts[i].x, capY), col, Vector2.zero);
        }
        for (int i = 0; i < n; i++)
        {
            int v0 = i * 2;
            vh.AddTriangle(v0, v0 + 1, v0 + 3);
            vh.AddTriangle(v0, v0 + 3, v0 + 2);
        }

        // 境界ライン
        if (drawEdgeLine && edgeLineWidth > 0f)
        {
            Color32 lc = edgeLineColor;
            int start = vh.currentVertCount;
            float hw = edgeLineWidth * 0.5f;
            for (int i = 0; i <= n; i++)
            {
                Vector2 prev = pts[Mathf.Max(0, i - 1)];
                Vector2 next = pts[Mathf.Min(n, i + 1)];
                Vector2 t = (next - prev).normalized;
                Vector2 nn = new Vector2(-t.y, t.x);
                vh.AddVert(pts[i] + nn * hw, lc, Vector2.zero);
                vh.AddVert(pts[i] - nn * hw, lc, Vector2.zero);
            }
            for (int i = 0; i < n; i++)
            {
                int v0 = start + i * 2;
                vh.AddTriangle(v0, v0 + 1, v0 + 3);
                vh.AddTriangle(v0, v0 + 3, v0 + 2);
            }
        }
    }

    private float Wave(float s)
    {
        const float TAU = Mathf.PI * 2f;
        float w = 0f;
        if (wavelength1 > 0f) w += amplitude1 * Mathf.Sin(TAU * (s / wavelength1 - time * speed1));
        if (wavelength2 > 0f) w += amplitude2 * Mathf.Sin(TAU * (s / wavelength2 - time * speed2) + 1.3f);
        if (wavelength3 > 0f) w += amplitude3 * Mathf.Sin(TAU * (s / wavelength3 - time * speed3) + 2.7f);
        return w;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
