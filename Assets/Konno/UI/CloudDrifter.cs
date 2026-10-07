using UnityEngine;

/// <summary>
/// 背景の雲を横に流す(ワールド空間のSpriteRendererを使うので、Canvasと違ってキャラの後ろに置ける)。
/// 空のGameObjectにアタッチし、Cloud Spritesに雲の画像(Sprite)を入れるだけで動く。
/// 雲は画面外に出たら反対側へ戻り、高さ・大きさ・画像を選び直して流れ続ける。
/// 小さい雲ほど遅く・薄くなり、奥行き(パララックス)が出る。
/// </summary>
public class CloudDrifter : MonoBehaviour
{
    [Header("雲の画像")]
    [Tooltip("雲のSprite(複数入れるとランダムに選ぶ)。ImportのTexture TypeはSprite (2D and UI)にする")]
    [SerializeField] private Sprite[] cloudSprites;

    [Header("数・範囲")]
    [SerializeField, Min(1)] private int cloudCount = 8;
    [Tooltip("ONにすると、カメラに映る範囲から横幅を自動で決める")]
    [SerializeField] private bool useCameraBounds = true;
    [SerializeField] private Camera targetCamera;
    [Tooltip("画面の外側にどれだけ余白を持たせるか(雲が端で急に消えないように)")]
    [SerializeField] private float edgeMargin = 3f;
    [Tooltip("Use Camera BoundsがOFFの時の左端・右端(ワールドX)")]
    [SerializeField] private float manualMinX = -12f;
    [SerializeField] private float manualMaxX = 12f;
    [Tooltip("雲を出す高さの範囲(ワールドY)")]
    [SerializeField] private float minY = 1f;
    [SerializeField] private float maxY = 4f;

    [Header("動き")]
    [Tooltip("ONで右へ、OFFで左へ流れる")]
    [SerializeField] private bool moveRight = false;
    [SerializeField] private float minSpeed = 0.3f;
    [SerializeField] private float maxSpeed = 0.9f;
    [Tooltip("ONにすると、KO演出のスロー(Time.timeScale)や一時停止の影響を受けず流れ続ける")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("見た目")]
    [SerializeField] private float minScale = 0.6f;
    [SerializeField] private float maxScale = 1.2f;
    [Tooltip("小さい(遠い)雲ほど薄くする。1=全部同じ濃さ")]
    [SerializeField, Range(0f, 1f)] private float farAlpha = 0.7f;
    [Tooltip("Sorting Layer(キャラより後ろになる層を指定)")]
    [SerializeField] private string sortingLayerName = "Default";
    [Tooltip("Order in Layer(小さいほど奥。キャラより小さい値にする)")]
    [SerializeField] private int sortingOrder = -50;

    private class Cloud
    {
        public Transform tr;
        public SpriteRenderer sr;
        public float speed;
        public float halfWidth;
    }

    private Cloud[] clouds;

    private void Start()
    {
        if (cloudSprites == null || cloudSprites.Length == 0)
        {
            Debug.LogError("[CloudDrifter] Cloud Sprites が空です。雲の画像を入れてください。", this);
            enabled = false;
            return;
        }

        if (targetCamera == null) targetCamera = Camera.main;

        clouds = new Cloud[cloudCount];
        GetRange(out float left, out float right);

        for (int i = 0; i < cloudCount; i++)
        {
            GameObject go = new GameObject("Cloud_" + i);
            go.transform.SetParent(transform, false);

            Cloud c = new Cloud
            {
                tr = go.transform,
                sr = go.AddComponent<SpriteRenderer>()
            };
            c.sr.sortingLayerName = sortingLayerName;

            Randomize(c);

            // 最初は画面全体にばらけて配置する(一斉に端から出てこないように)
            c.tr.position = new Vector3(Random.Range(left, right), c.tr.position.y, transform.position.z);
            clouds[i] = c;
        }
    }

    private void Update()
    {
        if (clouds == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float dir = moveRight ? 1f : -1f;
        GetRange(out float left, out float right);

        foreach (Cloud c in clouds)
        {
            Vector3 p = c.tr.position;
            p.x += dir * c.speed * dt;
            c.tr.position = p;

            // 完全に画面外へ出たら、反対側の外へ戻して新しい雲として再利用する
            if (moveRight && p.x - c.halfWidth > right)
            {
                Randomize(c);
                c.tr.position = new Vector3(left - c.halfWidth, c.tr.position.y, p.z);
            }
            else if (!moveRight && p.x + c.halfWidth < left)
            {
                Randomize(c);
                c.tr.position = new Vector3(right + c.halfWidth, c.tr.position.y, p.z);
            }
        }
    }

    /// <summary>画像・大きさ・高さ・速さ・濃さを選び直す。大きい雲ほど手前(速く濃く)にする。</summary>
    private void Randomize(Cloud c)
    {
        c.sr.sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];

        float t = Random.value; // 0=遠い(小さい) 〜 1=近い(大きい)
        float scale = Mathf.Lerp(minScale, maxScale, t);
        c.tr.localScale = new Vector3(scale, scale, 1f);

        c.speed = Mathf.Lerp(minSpeed, maxSpeed, t);

        Color col = c.sr.color;
        col.a = Mathf.Lerp(farAlpha, 1f, t);
        c.sr.color = col;

        // 大きい(近い)雲を少し手前に描く
        c.sr.sortingOrder = sortingOrder + Mathf.RoundToInt(t * 10f);

        c.halfWidth = c.sr.sprite.bounds.extents.x * scale;

        Vector3 pos = c.tr.position;
        pos.y = Random.Range(minY, maxY);
        c.tr.position = pos;
    }

    private void GetRange(out float left, out float right)
    {
        if (useCameraBounds && targetCamera != null && targetCamera.orthographic)
        {
            float halfW = targetCamera.orthographicSize * targetCamera.aspect;
            float cx = targetCamera.transform.position.x;
            left = cx - halfW - edgeMargin;
            right = cx + halfW + edgeMargin;
        }
        else
        {
            left = manualMinX;
            right = manualMaxX;
        }
    }
}
