using UnityEngine;

/// <summary>
/// SpriteRenderer で作った「帯」(タイトル画面の黒い斜めの帯など)の上下のふちを、
/// グネグネ波打たせて表示するスクリプト(UIではなく、スプライトと同じ2Dの世界に描く)。
///
/// 使い方:
///   1. 黒い帯のスプライト(例: Title_Back2)の「子」に空のオブジェクトを作る
///      (Transform は Position 0,0,0 / Rotation 0,0,0 / Scale 1,1,1 のまま)
///   2. そこにこのスクリプトを付け、Source に黒い帯の SpriteRenderer を入れる
///   3. 再生すると、元の帯は非表示になり、代わりに波打つ帯が同じ位置・角度・大きさで表示される
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WavyBandSprite : MonoBehaviour
{
    [Header("元の帯")]
    [Tooltip("形・大きさ・色・表示順をコピーする元の SpriteRenderer(黒い帯)")]
    [SerializeField] private SpriteRenderer source;
    [Tooltip("再生中は元の帯を非表示にする")]
    [SerializeField] private bool hideSourceInPlay = true;
    [Tooltip("ONなら色を元の帯からコピー。OFFなら下の Color を使う")]
    [SerializeField] private bool useSourceColor = true;
    [SerializeField] private Color color = Color.black;

    [Tooltip("Source を使わない場合の帯の大きさ(この オブジェクト内の単位)")]
    [SerializeField] private Vector2 manualSize = new Vector2(20f, 3f);

    [Header("波打たせるふち")]
    [SerializeField] private bool waveTop = true;
    [SerializeField] private bool waveBottom = true;
    [SerializeField, Range(16, 512)] private int segments = 200;

    [Header("波(高さは帯の太さに対する割合、長さは帯の長さに対する割合)")]
    [SerializeField] private float amplitude1 = 0.06f;
    [SerializeField] private float wavelength1 = 0.22f;
    [SerializeField] private float speed1 = 0.35f;

    [SerializeField] private float amplitude2 = 0.03f;
    [SerializeField] private float wavelength2 = 0.08f;
    [SerializeField] private float speed2 = -0.8f;

    [SerializeField] private float amplitude3 = 0.015f;
    [SerializeField] private float wavelength3 = 0.035f;
    [SerializeField] private float speed3 = 1.4f;

    [Tooltip("下のふちの波を上のふちとずらす量(0~1)。0だと上下が同じ形で揺れる")]
    [SerializeField, Range(0f, 1f)] private float bottomPhaseOffset = 0.37f;

    [Header("表示")]
    [Tooltip("未設定なら Sprites/Default を自動で使う")]
    [SerializeField] private Material material;
    [Tooltip("元の帯の表示順に足す値")]
    [SerializeField] private int sortingOrderOffset = 0;
    [SerializeField] private bool useUnscaledTime = true;

    private Mesh mesh;
    private Material runtimeMaterial;
    private MeshRenderer mr;
    private float time;
    private Vector3[] verts;
    private Color32[] cols;
    private int[] tris;

    private void OnEnable()
    {
        mr = GetComponent<MeshRenderer>();
        if (mesh == null)
        {
            mesh = new Mesh { name = "WavyBand", hideFlags = HideFlags.DontSave };
            mesh.MarkDynamic();
        }
        GetComponent<MeshFilter>().sharedMesh = mesh;

        if (material != null)
        {
            mr.sharedMaterial = material;
        }
        else
        {
            if (runtimeMaterial == null)
            {
                var sh = Shader.Find("Sprites/Default");
                if (sh != null)
                    runtimeMaterial = new Material(sh) { name = "WavyBand (Sprites/Default)", hideFlags = HideFlags.DontSave };
            }
            mr.sharedMaterial = runtimeMaterial;
        }

        if (Application.isPlaying && hideSourceInPlay && source != null)
            source.enabled = false;

        Rebuild();
    }

    private void OnDisable()
    {
        if (Application.isPlaying && hideSourceInPlay && source != null)
            source.enabled = true;
    }

    private void Update()
    {
        if (Application.isPlaying)
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        Rebuild();
    }

    private void Rebuild()
    {
        if (mesh == null || mr == null) return;

        // ---- 帯の大きさ・中心(このオブジェクトのローカル単位) ----
        Vector2 size = manualSize;
        Vector2 center = Vector2.zero;

        if (source != null && source.sprite != null)
        {
            if (source.drawMode == SpriteDrawMode.Simple)
            {
                Bounds b = source.sprite.bounds;
                size = b.size;
                center = b.center;
            }
            else
            {
                size = source.size;
                Vector2 pivot01 = source.sprite.pivot / source.sprite.rect.size;
                center = (new Vector2(0.5f, 0.5f) - pivot01) * size;
            }

            // 表示順を合わせる
            mr.sortingLayerID = source.sortingLayerID;
            mr.sortingOrder = source.sortingOrder + sortingOrderOffset;
        }

        Color c = (useSourceColor && source != null) ? source.color : color;
        Color32 c32 = c;

        // ---- 頂点 ----
        int n = Mathf.Max(2, segments);
        int vCount = (n + 1) * 2;
        if (verts == null || verts.Length != vCount)
        {
            verts = new Vector3[vCount];
            cols = new Color32[vCount];
            tris = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                int v = i * 2, t = i * 6;
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 3;
                tris[t + 3] = v; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
            }
        }

        float halfW = size.x * 0.5f, halfH = size.y * 0.5f;
        for (int i = 0; i <= n; i++)
        {
            float u = (float)i / n;
            float x = center.x - halfW + size.x * u;

            float top = center.y + halfH + (waveTop ? Wave(u, 0f) * size.y : 0f);
            float bottom = center.y - halfH + (waveBottom ? Wave(u, bottomPhaseOffset) * size.y : 0f);

            verts[i * 2] = new Vector3(x, top, 0f);
            verts[i * 2 + 1] = new Vector3(x, bottom, 0f);
            cols[i * 2] = c32;
            cols[i * 2 + 1] = c32;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.colors32 = cols;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
    }

    // u: 帯の長さ方向 0~1 / 戻り値: 帯の太さに対する割合
    private float Wave(float u, float phase)
    {
        const float TAU = Mathf.PI * 2f;
        float w = 0f;
        if (wavelength1 > 0f) w += amplitude1 * Mathf.Sin(TAU * (u / wavelength1 - time * speed1 + phase));
        if (wavelength2 > 0f) w += amplitude2 * Mathf.Sin(TAU * (u / wavelength2 - time * speed2 + phase * 1.7f) + 1.3f);
        if (wavelength3 > 0f) w += amplitude3 * Mathf.Sin(TAU * (u / wavelength3 - time * speed3 + phase * 2.3f) + 2.7f);
        return w;
    }
}
