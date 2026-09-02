using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaveFill : MonoBehaviour
{
    [Header("範囲")]
    public float lineWidth = 8f;
    public int segmentCount = 50;

    [Header("波の形状")]
    public float amplitude = 0.2f;
    public float wavelength = 2f;
    public float speed = 1.5f;

    [Header("帯の位置(中心Yと厚み)")]
    public float centerY = 0f;
    public float thickness = 2f; // 上端と下端の距離

    [Header("見た目")]
    public Color fillColor = new Color(0.3f, 0.6f, 0.9f); // 元の四角形と同じ青

    private Mesh mesh;
    private Vector3[] vertices;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = fillColor;
        GetComponent<MeshRenderer>().material = mat;

        vertices = new Vector3[segmentCount * 2];

        // 先に初期頂点を計算してセットする(ここを追加)
        UpdateVertices();
        mesh.vertices = vertices;

        // その後で三角形を設定する
        BuildTriangles();
    }
    void UpdateVertices()
    {
        float halfThick = thickness / 2f;

        for (int i = 0; i < segmentCount; i++)
        {
            float tt = (float)i / (segmentCount - 1);
            float x = Mathf.Lerp(-lineWidth / 2f, lineWidth / 2f, tt);
            float wave = Mathf.Sin((x / wavelength) + Time.time * speed) * amplitude;

            vertices[i * 2] = new Vector3(x, centerY + halfThick + wave, 0);
            vertices[i * 2 + 1] = new Vector3(x, centerY - halfThick + wave, 0);
        }
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

    void Update()
    {
        UpdateVertices();
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }
}