using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトルシーン開始時に、画面中央から画面を徐々に展開する演出。
/// 起動時に最前面へ黒い板を4枚作り、中央の「穴」を広げていく。
/// 終わると板は自動で消える。
/// </summary>
public class TitleOpenReveal : MonoBehaviour
{
    public enum RevealMode
    {
        LineThenOpen, // 中央に横線が伸びる → 上下に開く(ブラウン管風)
        Box,          // 中央の四角が縦横同時に広がる
        Horizontal,   // 左右に開く
        Vertical,     // 上下に開く
        Tiles,        // 画面をタイルに分けて、パラパラとめくれるように消える
        Blinds,       // 横長の帯(ブラインド)がパラパラとめくれる
        BlindsVertical// 縦長の帯がパラパラとめくれる
    }

    public enum TileOrder
    {
        FromCenter,   // 中央から外側へ
        Random,       // ランダム
        Diagonal,     // 左上から右下へ
        LeftToRight,  // 左から右へ
        TopToBottom   // 上から下へ
    }

    public enum TileEffect
    {
        Flip,         // カードのように縦にめくれる
        FlipX,        // 横にめくれる
        Shrink,       // 小さくなって消える
        Fade,         // 薄くなって消える
        Spin          // 回りながら小さくなる
    }

    [Header("演出")]
    [SerializeField] private RevealMode mode = RevealMode.LineThenOpen;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startDelay = 0.15f;
    [Tooltip("LineThenOpen の横線が伸びる時間")]
    [SerializeField] private float lineDuration = 0.35f;
    [Tooltip("開く時間")]
    [SerializeField] private float openDuration = 0.45f;
    [Tooltip("LineThenOpen の横線の太さ(画面高さに対する割合)")]
    [SerializeField, Range(0.001f, 0.1f)] private float lineThickness = 0.006f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("パラパラ(Tiles / Blinds)")]
    [Tooltip("横方向のタイル数(縦は画面比率から自動)。Blinds では帯の本数")]
    [SerializeField, Range(1, 64)] private int tileColumns = 16;
    [SerializeField] private TileOrder tileOrder = TileOrder.FromCenter;
    [SerializeField] private TileEffect tileEffect = TileEffect.Flip;
    [Tooltip("最初のタイルから最後のタイルが動き出すまでの時間")]
    [SerializeField] private float tileSpread = 0.6f;
    [Tooltip("1枚のタイルが消えるまでの時間")]
    [SerializeField] private float tileDuration = 0.22f;
    [Tooltip("順番のバラつき(0で整然、1でかなりバラバラ)")]
    [SerializeField, Range(0f, 1f)] private float tileJitter = 0.25f;
    [Tooltip("めくれる途中で一瞬光る色(Alpha 0で無効)")]
    [SerializeField] private Color tileFlashColor = new Color(1f, 1f, 1f, 0.6f);

    [Header("見た目")]
    [SerializeField] private Color coverColor = Color.black;
    [SerializeField] private bool showEdgeLines = true;
    [SerializeField] private Color edgeLineColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private float edgeLineWidth = 4f;
    [SerializeField] private int sortingOrder = 1000;

    public bool IsFinished { get; private set; }
    public event Action OnFinished;

    private GameObject canvasObj;
    private RectTransform top, bottom, left, right;
    private RectTransform lineTop, lineBottom, lineLeft, lineRight;
    private Image[] lineImages;

    private RectTransform[] tiles;
    private Image[] tileImages;
    private float[] tileDelays;

    private bool IsTileMode => mode == RevealMode.Tiles || mode == RevealMode.Blinds || mode == RevealMode.BlindsVertical;

    private void Awake()
    {
        Build();
        SetOpening(0f, 0f, 1f);
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        StopAllCoroutines();
        if (canvasObj == null) Build();
        IsFinished = false;
        StartCoroutine(RevealRoutine());
    }

    private float Dt => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private IEnumerator RevealRoutine()
    {
        SetOpening(0f, 0f, 1f);

        float d = 0f;
        while (d < startDelay) { d += Dt; yield return null; }

        switch (mode)
        {
            case RevealMode.LineThenOpen:
                yield return Animate(lineDuration, k =>
                {
                    float w = EaseOutQuart(k);
                    SetOpening(w, lineThickness, 1f);
                });
                yield return Animate(openDuration, k =>
                {
                    float h = Mathf.Lerp(lineThickness, 1f, EaseInOutCubic(k));
                    SetOpening(1f, h, 1f - k);
                });
                break;

            case RevealMode.Box:
                yield return Animate(openDuration, k =>
                {
                    float e = EaseOutQuart(k);
                    SetOpening(e, e, 1f - k);
                });
                break;

            case RevealMode.Horizontal:
                yield return Animate(openDuration, k => SetOpening(EaseOutQuart(k), 1f, 1f - k));
                break;

            case RevealMode.Vertical:
                yield return Animate(openDuration, k => SetOpening(1f, EaseOutQuart(k), 1f - k));
                break;

            case RevealMode.Tiles:
            case RevealMode.Blinds:
            case RevealMode.BlindsVertical:
                yield return TileRoutine();
                break;
        }

        SetOpening(1f, 1f, 0f);
        Destroy(canvasObj);
        canvasObj = null;

        IsFinished = true;
        OnFinished?.Invoke();
    }

    private IEnumerator Animate(float duration, Action<float> step)
    {
        float t = 0f;
        duration = Mathf.Max(0.0001f, duration);
        while (t < duration)
        {
            t += Dt;
            step(Mathf.Clamp01(t / duration));
            yield return null;
        }
        step(1f);
    }

    // w,h : 開いている穴の大きさ(画面に対する割合 0~1)
    private void SetOpening(float w, float h, float lineAlpha)
    {
        if (canvasObj == null || top == null) return;

        float x0 = 0.5f - w * 0.5f, x1 = 0.5f + w * 0.5f;
        float y0 = 0.5f - h * 0.5f, y1 = 0.5f + h * 0.5f;

        SetAnchors(top, new Vector2(0f, y1), new Vector2(1f, 1f));
        SetAnchors(bottom, new Vector2(0f, 0f), new Vector2(1f, y0));
        SetAnchors(left, new Vector2(0f, y0), new Vector2(x0, y1));
        SetAnchors(right, new Vector2(x1, y0), new Vector2(1f, y1));

        if (!showEdgeLines) return;

        SetLine(lineTop, new Vector2(x0, y1), new Vector2(x1, y1), new Vector2(0f, edgeLineWidth));
        SetLine(lineBottom, new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(0f, edgeLineWidth));
        SetLine(lineLeft, new Vector2(x0, y0), new Vector2(x0, y1), new Vector2(edgeLineWidth, 0f));
        SetLine(lineRight, new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(edgeLineWidth, 0f));

        bool sideVisible = w < 0.999f;
        lineLeft.gameObject.SetActive(sideVisible && h > lineThickness * 1.5f);
        lineRight.gameObject.SetActive(sideVisible && h > lineThickness * 1.5f);

        foreach (var img in lineImages)
        {
            Color c = edgeLineColor;
            c.a *= Mathf.Clamp01(lineAlpha);
            img.color = c;
        }
    }

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetLine(RectTransform rt, Vector2 min, Vector2 max, Vector2 size)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    private void Build()
    {
        canvasObj = new GameObject("TitleRevealCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        if (IsTileMode)
        {
            BuildTiles();
            return;
        }

        top = MakeRect("Top", coverColor, true);
        bottom = MakeRect("Bottom", coverColor, true);
        left = MakeRect("Left", coverColor, true);
        right = MakeRect("Right", coverColor, true);

        lineTop = MakeRect("LineTop", edgeLineColor, false);
        lineBottom = MakeRect("LineBottom", edgeLineColor, false);
        lineLeft = MakeRect("LineLeft", edgeLineColor, false);
        lineRight = MakeRect("LineRight", edgeLineColor, false);
        lineImages = new[]
        {
            lineTop.GetComponent<Image>(), lineBottom.GetComponent<Image>(),
            lineLeft.GetComponent<Image>(), lineRight.GetComponent<Image>()
        };

        if (!showEdgeLines)
        {
            lineTop.gameObject.SetActive(false);
            lineBottom.gameObject.SetActive(false);
            lineLeft.gameObject.SetActive(false);
            lineRight.gameObject.SetActive(false);
        }
    }

    private RectTransform MakeRect(string name, Color color, bool blockRaycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasObj.transform, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = blockRaycast; // 展開中は下のボタンを押せないようにする
        return (RectTransform)go.transform;
    }

    // ================= パラパラ =================
    private void BuildTiles()
    {
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        int cols, rows;
        switch (mode)
        {
            case RevealMode.Blinds: cols = 1; rows = tileColumns; break;
            case RevealMode.BlindsVertical: cols = tileColumns; rows = 1; break;
            default:
                cols = tileColumns;
                rows = Mathf.Max(1, Mathf.RoundToInt(tileColumns / aspect));
                break;
        }

        int count = cols * rows;
        tiles = new RectTransform[count];
        tileImages = new Image[count];
        tileDelays = new float[count];

        var rng = new System.Random();
        float maxDist = new Vector2(0.5f, 0.5f).magnitude;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int i = y * cols + x;
                var rt = MakeRect($"Tile_{x}_{y}", coverColor, true);
                rt.anchorMin = new Vector2((float)x / cols, (float)y / rows);
                rt.anchorMax = new Vector2((float)(x + 1) / cols, (float)(y + 1) / rows);
                rt.offsetMin = new Vector2(-1f, -1f); // 隙間が出ないよう少し重ねる
                rt.offsetMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                tiles[i] = rt;
                tileImages[i] = rt.GetComponent<Image>();

                // タイル中心(0~1)
                float cx = (x + 0.5f) / cols;
                float cy = (y + 0.5f) / rows;
                float order;
                switch (tileOrder)
                {
                    case TileOrder.Random: order = (float)rng.NextDouble(); break;
                    case TileOrder.Diagonal: order = (cx + (1f - cy)) * 0.5f; break;
                    case TileOrder.LeftToRight: order = cx; break;
                    case TileOrder.TopToBottom: order = 1f - cy; break;
                    default: order = new Vector2(cx - 0.5f, cy - 0.5f).magnitude / maxDist; break;
                }
                order = Mathf.Clamp01(order + ((float)rng.NextDouble() - 0.5f) * tileJitter);
                tileDelays[i] = order * tileSpread;
            }
        }
    }

    private IEnumerator TileRoutine()
    {
        if (tiles == null) yield break;

        float total = tileSpread + tileDuration;
        float t = 0f;
        while (t < total)
        {
            t += Dt;
            for (int i = 0; i < tiles.Length; i++)
                UpdateTile(i, Mathf.Clamp01((t - tileDelays[i]) / Mathf.Max(0.0001f, tileDuration)));
            yield return null;
        }
        for (int i = 0; i < tiles.Length; i++) UpdateTile(i, 1f);
    }

    // k: 0=覆っている 1=消えた
    private void UpdateTile(int i, float k)
    {
        var rt = tiles[i];
        var img = tileImages[i];
        if (k >= 1f)
        {
            if (rt.gameObject.activeSelf) rt.gameObject.SetActive(false);
            return;
        }

        float e = k * k; // 最初ゆっくり→最後に一気に
        Color c = coverColor;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        switch (tileEffect)
        {
            case TileEffect.Flip:
                rt.localScale = new Vector3(1f, Mathf.Cos(e * Mathf.PI * 0.5f), 1f);
                break;
            case TileEffect.FlipX:
                rt.localScale = new Vector3(Mathf.Cos(e * Mathf.PI * 0.5f), 1f, 1f);
                break;
            case TileEffect.Shrink:
                float sc = 1f - e;
                rt.localScale = new Vector3(sc, sc, 1f);
                break;
            case TileEffect.Fade:
                c.a *= 1f - e;
                break;
            case TileEffect.Spin:
                float sp = 1f - e;
                rt.localScale = new Vector3(sp, sp, 1f);
                rt.localRotation = Quaternion.Euler(0f, 0f, e * 180f);
                break;
        }

        // めくれる途中で一瞬光る
        if (tileFlashColor.a > 0f && k > 0f)
        {
            float flash = Mathf.Sin(k * Mathf.PI) * tileFlashColor.a;
            Color fc = tileFlashColor; fc.a = c.a;
            c = Color.Lerp(c, fc, flash);
        }
        img.color = c;
    }

    private static float EaseOutQuart(float k) => 1f - Mathf.Pow(1f - k, 4f);
    private static float EaseInOutCubic(float k) =>
        k < 0.5f ? 4f * k * k * k : 1f - Mathf.Pow(-2f * k + 2f, 3f) * 0.5f;

    private void OnDestroy()
    {
        if (canvasObj != null) Destroy(canvasObj);
    }
}