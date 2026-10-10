using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI用:上下のふちがグネグネ波打つ「帯」。Image と同じように Color で色を付けられる。
///
/// ・帯の長さ = RectTransform の Width、太さ = Height
/// ・斜めにしたい時は RectTransform の Rotation Z を回す
/// ・Hierarchy で下にあるものほど手前に描かれる(Image と同じ)
///
/// 例(タイトル背景):
///   BG_Blue   … Image(青・画面全体)
///   Wave      … WavyBandGraphic(水色など・黒帯より少し太く)
///   BlackBand … Image(黒・斜め)   ← Wave より下(手前)に置く
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class WavyBandGraphic : MaskableGraphic
{
    [Header("波打たせるふち")]
    [SerializeField] private bool waveTop = true;
    [SerializeField] private bool waveBottom = true;
    [SerializeField, Range(16, 512)] private int segments = 200;

    [Header("波1(大きなうねり) ※単位はピクセル")]
    [SerializeField] private float amplitude1 = 14f;
    [SerializeField] private float wavelength1 = 300f;
    [SerializeField] private float speed1 = 0.4f;

    [Header("波2(細かいグネグネ)")]
    [SerializeField] private float amplitude2 = 6f;
    [SerializeField] private float wavelength2 = 110f;
    [SerializeField] private float speed2 = -0.9f;

    [Header("波3(ゆらぎ)")]
    [SerializeField] private float amplitude3 = 3f;
    [SerializeField] private float wavelength3 = 45f;
    [SerializeField] private float speed3 = 1.6f;

    [Tooltip("下のふちの波を上のふちとずらす量(0~1)")]
    [SerializeField, Range(0f, 1f)] private float bottomPhaseOffset = 0.37f;

    [Header("アニメーション")]
    [SerializeField] private bool animate = true;
    [SerializeField] private bool useUnscaledTime = true;

    private float time;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false; // 背景なのでクリックを吸わない
    }

    private void Update()
    {
        if (!animate || !Application.isPlaying) return;
        time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0f || r.height <= 0f) return;

        int n = Mathf.Max(2, segments);
        Color32 col = color;

        for (int i = 0; i <= n; i++)
        {
            float u = (float)i / n;
            float x = r.xMin + r.width * u;
            float s = r.width * u; // 左端からの距離(px)

            float top = r.yMax + (waveTop ? Wave(s, 0f) : 0f);
            float bottom = r.yMin + (waveBottom ? Wave(s, bottomPhaseOffset) : 0f);

            vh.AddVert(new Vector3(x, top), col, new Vector2(u, 1f));
            vh.AddVert(new Vector3(x, bottom), col, new Vector2(u, 0f));
        }

        for (int i = 0; i < n; i++)
        {
            int v = i * 2;
            vh.AddTriangle(v, v + 1, v + 3);
            vh.AddTriangle(v, v + 3, v + 2);
        }
    }

    private float Wave(float s, float phase)
    {
        const float TAU = Mathf.PI * 2f;
        float w = 0f;
        if (wavelength1 > 0f) w += amplitude1 * Mathf.Sin(TAU * (s / wavelength1 - time * speed1 + phase));
        if (wavelength2 > 0f) w += amplitude2 * Mathf.Sin(TAU * (s / wavelength2 - time * speed2 + phase * 1.7f) + 1.3f);
        if (wavelength3 > 0f) w += amplitude3 * Mathf.Sin(TAU * (s / wavelength3 - time * speed3 + phase * 2.3f) + 2.7f);
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