using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 格闘ゲームのように、HPバーの横に「選ばれたキャラの顔」を表示する。
/// キャラ選択画面で決めたキャラ(SelectedFaceData)の顔画像を自動で読み込む。
///
/// 付属の演出(すべて任意):
///   ・ダメージを受けた瞬間に顔が揺れて、赤く光る(HPBarのOnRatioChangedで検知)
///   ・HPが少ない間、顔が赤みを帯びる
///   ・HPが0になると、顔が暗くなる(KO)
///   ・向きは、P1は右向き、P2/CPUは左向きに自動で揃える(元画像の向きはSource Faces Rightで指定)
///
/// 使い方:
///   1. HPバーの横に UI > Image を1つ作る(これが顔)。サイズは顔の枠に合わせる
///   2. この Image にこのコンポーネントを付ける(Face Image は空なら自分自身)
///   3. Player Number を 1 か 2 にする
///   4. Hp Bar に、そのプレイヤーのHPBarを入れる
///   5. 枠や背景がほしければ、顔の下に別のImage(枠画像)を置く
/// </summary>
public class FighterPortrait : MonoBehaviour
{
    [Header("対象")]
    [Tooltip("1=Player1(左) 2=Player2(右)")]
    [SerializeField, Range(1, 2)] private int playerNumber = 1;
    [Tooltip("顔を表示するImage。空ならこのオブジェクトのImage")]
    [SerializeField] private Image faceImage;
    [Tooltip("キャラ選択を通らずバトルシーンだけ再生した時に出す予備の顔")]
    [SerializeField] private Sprite fallbackSprite;

    [Header("HPとの連携 (任意)")]
    [Tooltip("そのプレイヤーのHPBar。入れるとダメージ・ピンチ・KOの演出が動く")]
    [SerializeField] private HPBar hpBar;

    [Header("向き")]
    [Tooltip("元の顔画像が「右向き」ならON、「左向き」ならOFF。" +
             "P1は右向き、P2/CPUは左向きになるよう、必要な時だけ自動で左右反転する")]
    [SerializeField] private bool sourceFacesRight = true;

    [Header("ダメージ演出")]
    [SerializeField] private bool shakeOnDamage = true;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 8f;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float flashDuration = 0.2f;

    [Header("ピンチ・KO")]
    [Tooltip("HPがこの割合以下の間、顔が赤みを帯びる(0で無効)")]
    [SerializeField, Range(0f, 1f)] private float lowHpThreshold = 0.25f;
    [SerializeField] private Color lowHpTint = new Color(1f, 0.7f, 0.7f);
    [Tooltip("HPが0になった時の色(暗くなる)")]
    [SerializeField] private Color koTint = new Color(0.35f, 0.35f, 0.35f);

    private RectTransform faceRect;
    private Vector2 basePosition;
    private Vector3 baseScale;
    private float lastRatio = -1f;
    private float shakeTimer;
    private float flashTimer;
    private bool isKo;
    private bool isLow;

    private void Awake()
    {
        if (faceImage == null) faceImage = GetComponent<Image>();
        if (faceImage == null)
        {
            Debug.LogError("[FighterPortrait] Face Image がありません。ImageにこのコンポーネントをつけるかFace Imageを設定してください。", this);
            enabled = false;
            return;
        }

        faceRect = faceImage.rectTransform;
        basePosition = faceRect.anchoredPosition;
        baseScale = faceRect.localScale;
        faceImage.preserveAspect = true;
    }

    private void OnEnable()
    {
        if (hpBar != null)
        {
            hpBar.OnRatioChanged += HandleRatioChanged;
            hpBar.OnDepleted += HandleDepleted;
        }
    }

    private void OnDisable()
    {
        if (hpBar != null)
        {
            hpBar.OnRatioChanged -= HandleRatioChanged;
            hpBar.OnDepleted -= HandleDepleted;
        }
    }

    private void Start()
    {
        ApplyFace();
        ApplyFlip();
        RefreshColor();
    }

    /// <summary>選ばれたキャラの顔を読み込む。外部から再読み込みしたい時にも呼べる。</summary>
    public void ApplyFace()
    {
        if (faceImage == null) return;

        Sprite face = SelectedFaceData.Get(playerNumber);
        if (face == null) face = fallbackSprite;

        faceImage.sprite = face;
        faceImage.enabled = face != null;
    }

    private void ApplyFlip()
    {
        if (faceRect == null) return;
        // P1は右向き、P2/CPUは左向きにしたい。元画像の向きと違う時だけ反転する
        bool wantFaceRight = playerNumber == 1;
        bool flip = wantFaceRight != sourceFacesRight;

        Vector3 s = baseScale;
        s.x = Mathf.Abs(s.x) * (flip ? -1f : 1f);
        faceRect.localScale = s;
        baseScale = s;
    }

    private void HandleRatioChanged(float ratio)
    {
        // 最初の呼び出し(満タンの初期反映)は基準にするだけで、演出はしない
        if (lastRatio >= 0f && ratio < lastRatio - 0.0001f)
        {
            if (shakeOnDamage) shakeTimer = shakeDuration;
            flashTimer = flashDuration;
        }

        lastRatio = ratio;
        isKo = ratio <= 0f;
        isLow = !isKo && lowHpThreshold > 0f && ratio <= lowHpThreshold;
    }

    private void HandleDepleted()
    {
        isKo = true;
    }

    private void Update()
    {
        if (faceRect == null) return;

        float dt = Time.unscaledDeltaTime;

        // 揺れ
        if (shakeTimer > 0f)
        {
            shakeTimer -= dt;
            float k = Mathf.Clamp01(shakeTimer / Mathf.Max(0.0001f, shakeDuration));
            Vector2 jitter = Random.insideUnitCircle * shakeStrength * k;
            faceRect.anchoredPosition = basePosition + jitter;
        }
        else
        {
            faceRect.anchoredPosition = basePosition;
        }

        if (flashTimer > 0f) flashTimer -= dt;

        RefreshColor();
    }

    private void RefreshColor()
    {
        if (faceImage == null) return;

        Color baseColor = isKo ? koTint : (isLow ? lowHpTint : Color.white);

        if (flashTimer > 0f && !isKo)
        {
            float k = Mathf.Clamp01(flashTimer / Mathf.Max(0.0001f, flashDuration));
            baseColor = Color.Lerp(baseColor, damageFlashColor, k);
        }

        faceImage.color = baseColor;
    }
}