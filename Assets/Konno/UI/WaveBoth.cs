using UnityEngine;

public class WaveBoth : MonoBehaviour
{
    [Header("上下のLine Rendererを登録")]
    public LineRenderer topLine;
    public LineRenderer bottomLine;

    [Header("波線の範囲")]
    public float lineWidth = 8f;
    public int segmentCount = 50;

    [Header("波の形状(上下共通)")]
    public float amplitude = 0.2f;
    public float wavelength = 2f;
    public float speed = 1.5f;

    [Header("上下のオフセット距離")]
    public float topOffsetY = 1f;      // 四角形の上端
    public float bottomOffsetY = -1f;  // 四角形の下端

    void Start()
    {
        topLine.positionCount = segmentCount;
        bottomLine.positionCount = segmentCount;
        topLine.useWorldSpace = false;
        bottomLine.useWorldSpace = false;
    }

    void Update()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            float x = Mathf.Lerp(-lineWidth / 2f, lineWidth / 2f, t);

            // 同じsin計算を使うことで上下が完全に同期する
            float wave = Mathf.Sin((x / wavelength) + Time.time * speed) * amplitude;

            topLine.SetPosition(i, new Vector3(x, topOffsetY + wave, 0));
            bottomLine.SetPosition(i, new Vector3(x, bottomOffsetY + wave, 0));
        }
    }
}