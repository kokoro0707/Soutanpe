using UnityEngine;

public class WaveEdges : MonoBehaviour
{
    [Header("範囲(四角形の横幅に合わせる)")]
    public float lineWidth = 8f;
    public int segmentCount = 50;

    [Header("波の形状")]
    public float amplitude = 0.2f;
    public float wavelength = 2f;
    public float speed = 1.5f;

    [Header("上下の位置(四角形の上端・下端のY座標)")]
    public float topEdgeY = 1f;
    public float bottomEdgeY = -1f;

    [Header("見た目")]
    public float lineThickness = 0.05f;
    public Color lineColor = Color.blue;

    private LineRenderer topLine;
    private LineRenderer bottomLine;

    void Start()
    {
        topLine = CreateLine("WaveEdge_Top");
        bottomLine = CreateLine("WaveEdge_Bottom");
    }

    LineRenderer CreateLine(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = segmentCount;
        lr.useWorldSpace = false;
        lr.loop = false;

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = lineThickness;
        lr.endWidth = lineThickness;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.numCornerVertices = 5;
        lr.sortingOrder = 10;

        return lr;
    }

    void Update()
    {
        if (topLine == null || bottomLine == null) return;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            float x = Mathf.Lerp(-lineWidth / 2f, lineWidth / 2f, t);

            // 上端の波(独立)
            float waveTop = Mathf.Sin((x / wavelength) + Time.time * speed) * amplitude;
            topLine.SetPosition(i, new Vector3(x, topEdgeY + waveTop, 0));

            // 下端の波(独立、少し位相をずらす)
            float waveBottom = Mathf.Sin((x / wavelength) + Time.time * speed + 1f) * amplitude;
            bottomLine.SetPosition(i, new Vector3(x, bottomEdgeY + waveBottom, 0));
        }
    }
}