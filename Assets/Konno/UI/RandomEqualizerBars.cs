using UnityEngine;

public class RandomEqualizerBars : MonoBehaviour
{
    [Header("バーの設定")]
    public GameObject barPrefab;      // 細長い四角形のPrefab(SpriteRendererでOK)
    public int barCount = 12;         // バーの本数
    public float barSpacing = 0.6f;   // バー同士の間隔
    public float barWidth = 0.4f;     // バーの幅

    [Header("動きの設定")]
    public float minHeight = 0.3f;    // 最小の高さ
    public float maxHeight = 3f;      // 最大の高さ
    public float noiseSpeed = 1f;     // 動きの速さ(大きいほど激しく動く)

    private Transform[] bars;
    private float[] noiseOffsets;     // バーごとに違う乱数の種

    void Start()
    {
        bars = new Transform[barCount];
        noiseOffsets = new float[barCount];

        float startX = -(barCount - 1) * barSpacing / 2f;

        for (int i = 0; i < barCount; i++)
        {
            GameObject bar = Instantiate(barPrefab, transform);
            bar.transform.localPosition = new Vector3(startX + i * barSpacing, 0, 0);
            bar.transform.localScale = new Vector3(barWidth, minHeight, 1f);

            bars[i] = bar.transform;

            // バーごとにランダムな種をつける(これで動きがバラバラになる)
            noiseOffsets[i] = Random.Range(0f, 1000f);
        }
    }

    void Update()
    {
        for (int i = 0; i < barCount; i++)
        {
            // Perlin Noiseで滑らかにランダムな高さを作る
            float noise = Mathf.PerlinNoise(noiseOffsets[i], Time.time * noiseSpeed);
            float height = Mathf.Lerp(minHeight, maxHeight, noise);

            Vector3 scale = bars[i].localScale;
            scale.y = height;
            bars[i].localScale = scale;

            // バーの下端を基準にしたい場合は位置も調整
            Vector3 pos = bars[i].localPosition;
            pos.y = height / 2f; // 下端をY=0に固定
            bars[i].localPosition = pos;
        }
    }
}