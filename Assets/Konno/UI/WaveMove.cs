using UnityEngine;

public class WaveMove : MonoBehaviour
{
    [Header("移動設定")]
    public Vector2 startPos = new Vector2(-4f, -3f);   // 左下
    public Vector2 endPos = new Vector2(4f, 3f);        // 右上
    public float duration = 3f;                          // 移動にかかる秒数

    [Header("波設定")]
    public float waveHeight = 0.5f;   // 波の高さ
    public float waveSpeed = 5f;      // 波の速さ(周期)

    private float timer = 0f;

    void Start()
    {
        transform.position = startPos;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        // 直線移動(左下→右上)
        Vector2 linearPos = Vector2.Lerp(startPos, endPos, t);

        // 移動方向に対して垂直に波オフセットをかける
        Vector2 dir = (endPos - startPos).normalized;
        Vector2 perpendicular = new Vector2(-dir.y, dir.x); // 垂直方向

        float waveOffset = Mathf.Sin(timer * waveSpeed) * waveHeight;
        Vector2 finalPos = linearPos + perpendicular * waveOffset;

        transform.position = finalPos;

        if (t >= 1f)
        {
            // ループさせたい場合
            timer = 0f;
        }
    }
}