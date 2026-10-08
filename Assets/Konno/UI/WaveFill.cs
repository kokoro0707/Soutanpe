using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaveFill : MonoBehaviour
{
    [Header("範囲")]
    public float lineWidth = 8f;
    public int segmentCount = 50;

    [Header("上端の波(独立)")]
    public float topAmplitude = 0.2f;
    public float topWavelength = 2f;
    public float topSpeed = 1.5f;

    [Header("下端の波(独立)")]
    public float bottomAmplitude = 0.2f;
    public float bottomWavelength = 2f;
    public float bottomSpeed = -1.2f; // 上と違う速さ・向きにすると自然

    [Header("帯の位置(上端Y・下端Y)")]
    public float topEdgeY = 1f;
    public float bottomEdgeY = -1f;

    [Header("見た目")]
    [Tooltip("素材(WaveBand.png)を入れない時の単色。素材を入れた場合は、素材に掛け合わせる色(白=素材そのまま)")]
    public Color fillColor = new Color(0.3f, 0.6f, 0.9f);

    [Header("素材(任意)")]
    [Tooltip("WaveBand.png。Import設定で Wrap Mode を Repeat にすること。空なら従来どおり単色")]
    public Texture2D bandTexture;
    [Tooltip("画像1枚分の横幅(ワールド単位)。小さいほど模様が細かくなる")]
    public float textureWorldWidth = 4f;
    [Tooltip("模様が横に流れる速さ(1秒あたり、画像1枚分を1とする)。マイナスで逆向き")]
    public float textureScrollSpeed = 0.05f;

    [Header("描画順")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private Material mat;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mat = new Material(Shader.Find("Sprites/Default"));
        if (bandTexture != null)
        {
            bandTexture.wrapMode = TextureWrapMode.Repeat;
            mat.mainTexture = bandTexture;
            mat.color = (fillColor == new Color(0.3f, 0.6f, 0.9f)) ? Color.white : fillColor;
        }
        else
        {
            mat.color = fillColor;
        }

        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.material = mat;
        mr.sortingLayerName = sortingLayerName;
        mr.sortingOrder = sortingOrder;

        vertices = new Vector3[segmentCount * 2];
        uvs = new Vector2[segmentCount * 2];

        UpdateVertices();
        mesh.vertices = vertices;
        mesh.uv = uvs;

        BuildTriangles();
    }

    void BuildTriangles()
    {
        int[] triangles = new int[(segmentCount - 1) * 6];
        int t = 0;
        for (int i = 0; i < segmentCount - 1; i++)
        {
            int topA = i * 2;
            int botA = i * 2 + 1;
            int topB = (i + 1) * 2;
            int botB = (i + 1) * 2 + 1;

            triangles[t++] = topA; triangles[t++] = topB; triangles[t++] = botA;
            triangles[t++] = botA; triangles[t++] = topB; triangles[t++] = botB;
        }
        mesh.triangles = triangles;
    }

    void UpdateVertices()
    {
        float texW = Mathf.Max(textureWorldWidth, 0.01f);

        for (int i = 0; i < segmentCount; i++)
        {
            float tt = (float)i / (segmentCount - 1);
            float x = Mathf.Lerp(-lineWidth / 2f, lineWidth / 2f, tt);

            // 上端の波(下端とは完全に独立)
            float waveTop = Mathf.Sin((x / topWavelength) + Time.time * topSpeed) * topAmplitude;

            // 下端の波(上端とは完全に独立)
            float waveBottom = Mathf.Sin((x / bottomWavelength) + Time.time * bottomSpeed) * bottomAmplitude;

            vertices[i * 2] = new Vector3(x, topEdgeY + waveTop, 0);
            vertices[i * 2 + 1] = new Vector3(x, bottomEdgeY + waveBottom, 0);

            // 画像の縦方向(上端=1, 下端=0)を、帯の上端から下端にぴったり合わせる。
            // 波で上下端が動いても、縁の光る線は常に帯の縁に沿う。
            float u = x / texW;
            uvs[i * 2] = new Vector2(u, 1f);
            uvs[i * 2 + 1] = new Vector2(u, 0f);
        }
    }

    void Update()
    {
        UpdateVertices();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.RecalculateBounds();

        if (mat != null && bandTexture != null)
        {
            mat.mainTextureOffset = new Vector2(Time.time * textureScrollSpeed, 0f);
        }
    }
}