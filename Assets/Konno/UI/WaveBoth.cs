using UnityEngine;

public class WaveBoth : MonoBehaviour
{
    [Header("波線の範囲(四角形の横幅に合わせる)")]
    public float lineWidth = 8f;
    public int segmentCount = 50;

    [Header("波の形状(上下共通)")]
    public float amplitude = 0.2f;
    public float wavelength = 2f;
    public float speed = 1.5f;

    [Header("上下のオフセット距離(四角形の上端・下端のY座標)")]
    public float topOffsetY = 1f;
    public float bottomOffsetY = -1f;

    [Header("見た目")]
    public float lineThickness = 0.05f;
    public Color lineColor = Color.blue;

    private LineRenderer topLine;
    private LineRenderer bottomLine;

    void Start()
    {
        topLine = CreateLine("WaveLine_Top_Auto");
        bottomLine = CreateLine("WaveLine_Bottom_Auto");
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
        lr.sortingOrder = 10; // 四角形より手前に表示

        return lr;
    }

    void Update()
    {
        if (topLine == null || bottomLine == null) return;

        // 念のため毎フレーム点の数を同期させる
        topLine.positionCount = segmentCount;
        bottomLine.positionCount = segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            float x = Mathf.Lerp(-lineWidth / 2f, lineWidth / 2f, t);

            float wave = Mathf.Sin((x / wavelength) + Time.time * speed) * amplitude;

            topLine.SetPosition(i, new Vector3(x, topOffsetY + wave, 0));
            bottomLine.SetPosition(i, new Vector3(x, bottomOffsetY + wave, 0));
        }
    }
}