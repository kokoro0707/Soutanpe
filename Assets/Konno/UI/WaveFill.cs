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
    public Color fillColor = new Color(0.3f, 0.6f, 0.9f);

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

        UpdateVertices();
        mesh.vertices = vertices;

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
        }
    }

    void Update()
    {
        UpdateVertices();
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }
}