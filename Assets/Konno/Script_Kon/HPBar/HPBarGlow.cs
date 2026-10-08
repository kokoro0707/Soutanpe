using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HPバーを「白く発光している」見た目にする。HPBar本体のスクリプトは変更不要。
///
/// ・バーの後ろに、白いやわらかい光(ハロー)を自動生成して敷く(画像素材は不要)
/// ・光がゆっくり脈打つ(明滅)
/// ・任意でバー本体(Fill Image)も、光に合わせて白っぽく明るくする
///
/// 使い方: HPバーの枠(または背景)のRectTransformにアタッチする。
/// 光らせたいバーが複数あるなら、それぞれにアタッチする。
/// </summary>
[DisallowMultipleComponent]
public class HPBarGlow : MonoBehaviour
{
    [Header("対象")]
    [Tooltip("光を出す範囲(バーの枠/背景)。空ならこのオブジェクト自身")]
    [SerializeField] private RectTransform target;

    [Header("光(ハロー)")]
    [SerializeField] private Color glowColor = Color.white;
    [Tooltip("バーの外側へ光がどれだけはみ出すか(ピクセル)")]
    [SerializeField, Min(0f)] private float padding = 20f;
    [Tooltip("光の強さ(0〜1)")]
    [SerializeField, Range(0f, 1f)] private float intensity = 0.8f;

    [Header("脈動")]
    [SerializeField] private bool pulse = true;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 2f;
    [Tooltip("明滅の振れ幅(0=ずっと一定)")]
    [SerializeField, Range(0f, 1f)] private float pulseAmount = 0.35f;
    [Tooltip("ONにすると、ポーズ中(timeScale=0)やKOスロー中も明滅する")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("バー本体を白く光らせる (任意)")]
    [Tooltip("HPバーのFill画像。入れると光に合わせて白っぽく明るくなる")]
    [SerializeField] private Image fillImage;
    [Tooltip("最大でどれだけ白に近づけるか(0〜1)")]
    [SerializeField, Range(0f, 1f)] private float fillWhiten = 0.25f;

    private Image glow;
    private Color fillBaseColor;
    private static Sprite cachedSprite;

    private void Awake()
    {
        if (target == null) target = transform as RectTransform;
        BuildGlow();
        if (fillImage != null) fillBaseColor = fillImage.color;
    }

    private void OnEnable()
    {
        if (glow != null) glow.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (glow != null) glow.gameObject.SetActive(false);
        if (fillImage != null) fillImage.color = fillBaseColor;
    }

    private void OnDestroy()
    {
        if (glow != null) Destroy(glow.gameObject);
    }

    private void LateUpdate()
    {
        if (glow == null) return;

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float wave = pulse ? (Mathf.Sin(t * pulseSpeed) * 0.5f + 0.5f) : 1f; // 0〜1
        float k = pulse ? Mathf.Lerp(1f - pulseAmount, 1f, wave) : 1f;

        Color c = glowColor;
        c.a = glowColor.a * intensity * k;
        glow.color = c;

        if (fillImage != null)
        {
            // 元の色の透明度は保ったまま、白へ寄せる
            Color w = Color.Lerp(fillBaseColor, Color.white, fillWhiten * wave);
            w.a = fillBaseColor.a;
            fillImage.color = w;
        }
    }

    /// <summary>バーの後ろ(同じ親の中で、対象の直前)に光のImageを作る。</summary>
    private void BuildGlow()
    {
        Transform parent = target.parent != null ? target.parent : target;

        GameObject go = new GameObject("HPBarGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = target.anchorMin;
        rt.anchorMax = target.anchorMax;
        rt.pivot = target.pivot;
        rt.anchoredPosition = target.anchoredPosition;
        rt.sizeDelta = target.sizeDelta + new Vector2(padding * 2f, padding * 2f);
        rt.localRotation = target.localRotation;
        rt.localScale = target.localScale;

        // バーの直前に差し込む=バーの後ろに描かれる
        if (target.parent != null) go.transform.SetSiblingIndex(target.GetSiblingIndex());

        // 親がレイアウトグループでも位置を崩さない
        go.AddComponent<LayoutElement>().ignoreLayout = true;

        glow = go.GetComponent<Image>();
        glow.sprite = GetGlowSprite();
        glow.type = Image.Type.Sliced;
        glow.raycastTarget = false;
    }

    /// <summary>やわらかい角丸の光を、コードで1枚だけ生成して使い回す(9スライス用)。</summary>
    private static Sprite GetGlowSprite()
    {
        if (cachedSprite != null) return cachedSprite;

        const int size = 64;
        const int border = 24; // 外側のにじみ部分
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float min = border, max = size - border;
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(min - (x + 0.5f), 0f, (x + 0.5f) - max);
                float dy = Mathf.Max(min - (y + 0.5f), 0f, (y + 0.5f) - max);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d / border);
                a = a * a * (3f - 2f * a); // なめらかに減衰
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();

        cachedSprite = Sprite.Create(
            tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        return cachedSprite;
    }
}
