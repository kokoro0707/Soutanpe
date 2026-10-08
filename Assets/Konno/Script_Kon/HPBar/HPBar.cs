using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 汎用HPバー。特定のキャラクタークラス(Fighter/HealthSystemなど)に依存しない。
///
/// 使い方:
///   自分のPlayer/EnemyのHPが変化したタイミングで、このコンポーネントの
///   SetHealth(現在HP, 最大HP) を呼び出すだけでよい。
///
///   例:
///     [SerializeField] private HPBar hpBar;
///     hpBar.SetHealth(currentHP, maxHP);
///
/// 見た目:
///   本体バー(hpSlider)は即座に反映され、ダメージバー(damageSlider)は
///   少し待ってからゆっくり追従する(残像のようなダメージ演出)。
///
/// Slider階層の作り方:
///   1. UI > Slider を2つ作る(HPSlider / DamageSlider)
///   2. どちらも Interactable は自動でOFFにされる(Awakeで設定)ので手動作業不要
///   3. どちらも子の Handle Slide Area は削除してOK(つまみ不要)
///   4. 手前に表示したい方(HPSlider)以外の Background は削除するか無効化する
///      (残すと奥のバーが隠れてしまう)
///   5. Hierarchy上で DamageSlider → HPSlider の順に並べる(下にある方が手前)
///
/// リザルト連携:
///   SetHealth() で ratio が0になったタイミングで OnDepleted イベントが発火する。
///   GameResultManager 側でこのイベントを購読すれば、キャラ側のスクリプトを
///   一切変更せずにHP0検知ができる。
/// </summary>
public class HPBar : MonoBehaviour
{
    [Header("スライダー参照")]
    [SerializeField] private Slider hpSlider;       // 即時反映される本体バー
    [SerializeField] private Slider damageSlider;    // 遅れて追従するダメージ表示バー
    [Header("演出設定")]
    [Tooltip("被ダメージ後、ダメージバーが減り始めるまでの待ち時間(秒)")]
    [SerializeField] private float delay = 0.4f;
    [Tooltip("ダメージバーが1秒間に減る割合(0から1のうちどれだけ進むか)")]
    [SerializeField] private float speed = 0.6f;
    [Header("ピンチ時の色変化(任意)")]
    [Tooltip("空でOK。中身を入れても、実行時に必ず Hp Slider の実際のFillで自動的に上書きされる(取り違え防止のため)")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private bool changeColorWhenLow = true;
    [SerializeField, Range(0f, 1f)] private float lowHpThreshold = 0.25f;
    [SerializeField] private Color normalColor = new Color(0.35f, 1f, 0.15f); // より明るい鮮やかな緑
    [SerializeField] private Color lowHpColor = new Color(1f, 0.2f, 0.15f);     // 明るい赤
    [Header("白い発光(任意)")]
    [Tooltip("ONにすると、HPバーの後ろに白いやわらかい光を出す(画像素材は不要。コードで自動生成)")]
    [SerializeField] private bool glowEnabled = true;
    [SerializeField] private Color glowColor = Color.white;
    [Tooltip("ピンチ時の光の色(赤みを足したい場合など)。白のままなら変化なし")]
    [SerializeField] private Color glowLowHpColor = new Color(1f, 0.75f, 0.7f);
    [Tooltip("光がバーの外へはみ出す量(ピクセル)")]
    [SerializeField, Min(0f)] private float glowPadding = 20f;
    [SerializeField, Range(0f, 1f)] private float glowIntensity = 0.8f;
    [SerializeField] private float glowPulseSpeed = 2f;
    [Tooltip("明滅の振れ幅(0=一定の光)")]
    [SerializeField, Range(0f, 1f)] private float glowPulseAmount = 0.35f;
    [Tooltip("ピンチ時に、明滅を何倍速くするか(1=変えない)")]
    [SerializeField, Min(1f)] private float lowHpPulseMultiplier = 2f;
    [Tooltip("バー本体も、光に合わせて白っぽく明るくする量(0=しない)")]
    [SerializeField, Range(0f, 1f)] private float fillWhiten = 0.25f;

    private Image glowImage;
    private float glowPhase;
    private static Sprite glowSprite;

    private float timer;

    // ===== ここから追加: HP0検知用 =====
    /// <summary>SetHealthで割合(ratio)が0になった瞬間に一度だけ発火する</summary>
    public event System.Action OnDepleted;
    private bool hasDepleted = false;
    // ===== 追加ここまで =====

    private void Awake()
    {
        // Slider側の設定ミスを防ぐため、必要な項目はコードから自動設定しておく
        foreach (var slider in new[] { hpSlider, damageSlider })
        {
            if (slider == null) continue;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.interactable = false; // マウス操作で動かせないようにする
            // InteractableをOFFにすると、Transitionが Color Tint のままだと
            // Unity側が自動でFillを薄暗く(無効化色に)してしまうため、Transitionを切る
            slider.transition = Selectable.Transition.None;
        }
        // hpSlider自身が「本当に描画に使っているFill」を必ず正として採用する。
        // Hp Fill Image に手動で違うImageがドラッグされていても、ここで上書きするため
        // 人為的な取り違えミスが起こりようがなくなる。
        if (hpSlider != null && hpSlider.fillRect != null)
        {
            var actualFill = hpSlider.fillRect.GetComponent<Image>();
            if (actualFill != null)
            {
                hpFillImage = actualFill;
            }
            else
            {
                Debug.LogWarning(
                    $"[HPBar] {hpSlider.name} の Fill Rect に Image コンポーネントが見つかりません。",
                    this);
            }
        }
        else if (hpFillImage == null)
        {
            Debug.LogWarning(
                $"[HPBar] {name} は Hp Slider が未設定、または Fill Rect が空のため、色変更ができません。",
                this);
        }
        // SetHealth()が呼ばれるまでの間、Editor上の元の色(黒など)が
        // 見えてしまわないよう、開始時点で先に通常色を反映しておく
        if (changeColorWhenLow && hpFillImage != null)
        {
            hpFillImage.color = normalColor;
        }

        if (glowEnabled) BuildGlow();
    }
    /// <summary>
    /// 外部の攻撃/回復スクリプトから、HPが変化するたびに呼び出す。
    /// </summary>
    public void SetHealth(float current, float max)
    {
        if (max <= 0f) max = 1f;
        float ratio = Mathf.Clamp01(current / max);
        Debug.Log($"[HPBar] {name} SetHealth current={current} max={max} ratio={ratio} hpSlider={(hpSlider != null ? hpSlider.name : "null")}", this);
        if (hpSlider != null)
        {
            // Sliderへコードから代入すると OnValueChanged が誤発火し得るため
            // SetValueWithoutNotify を使う(フィードバックループ対策)
            hpSlider.SetValueWithoutNotify(ratio);
            if (changeColorWhenLow && hpFillImage != null)
                hpFillImage.color = ratio <= lowHpThreshold ? lowHpColor : normalColor;
        }
        if (damageSlider == null)
        {
            CheckDepleted(ratio);
            return;
        }
        if (damageSlider.value <= ratio)
        {
            // 回復時、またはダメージバーがすでに追いついている場合は即座に同期
            damageSlider.SetValueWithoutNotify(ratio);
            timer = 0f;
        }
        // ダメージでバーが減った場合は、Update側で delay 後にゆっくり追従させる

        CheckDepleted(ratio);
    }

    /// <summary>
    /// ダメージバー(残像)の追従アニメーションを待たず、即座に本体バーの値へ同期させる。
    /// Time.timeScale = 0 でゲームを止める前に呼ぶことで、
    /// アニメーション途中の赤いバーが凍結して残ってしまう不具合を防げる。
    /// </summary>
    public void SyncDamageBarInstantly()
    {
        if (damageSlider == null || hpSlider == null) return;
        damageSlider.SetValueWithoutNotify(hpSlider.value);
        timer = 0f;
    }

    // ===== ここから追加: HP0検知用 =====
    private void CheckDepleted(float ratio)
    {
        if (ratio <= 0f)
        {
            if (!hasDepleted)
            {
                hasDepleted = true;
                OnDepleted?.Invoke();
            }
        }
        else
        {
            // 回復・シーン再読み込みなどでHPが戻った場合は再度検知できるようにリセット
            hasDepleted = false;
        }
    }
    // ===== 追加ここまで =====

    private void Update()
    {
        // 呼び出しタイミングのズレや参照の初期化順に関係なく、
        // 毎フレーム強制的に正しい色へ合わせる(黒残り対策)
        bool isLow = hpSlider != null && hpSlider.value <= lowHpThreshold;
        UpdateGlow(isLow);

        if (changeColorWhenLow && hpFillImage != null && hpSlider != null)
        {
            Color targetColor = isLow ? lowHpColor : normalColor;
            // 発光中は、光の明滅に合わせてバー本体も白へ寄せる
            if (glowImage != null && fillWhiten > 0f)
            {
                float wave = Mathf.Sin(glowPhase) * 0.5f + 0.5f;
                targetColor = Color.Lerp(targetColor, Color.white, fillWhiten * wave);
            }
            if (hpFillImage.color != targetColor)
            {
                hpFillImage.color = targetColor;
            }
        }
        if (hpSlider == null || damageSlider == null) return;
        if (damageSlider.value <= hpSlider.value) return;
        timer += Time.deltaTime;
        if (timer < delay) return;
        float newValue = Mathf.MoveTowards(damageSlider.value, hpSlider.value, speed * Time.deltaTime);
        damageSlider.SetValueWithoutNotify(newValue);
    }

    // ===== ここから追加: 白い発光 =====
    private void UpdateGlow(bool isLow)
    {
        if (glowImage == null) return;

        // 明滅はTime.timeScale=0(ポーズ・リザルト)中も止めない
        float speed = glowPulseSpeed * (isLow ? lowHpPulseMultiplier : 1f);
        glowPhase += speed * Time.unscaledDeltaTime;

        float wave = Mathf.Sin(glowPhase) * 0.5f + 0.5f;
        float k = Mathf.Lerp(1f - glowPulseAmount, 1f, wave);

        Color c = isLow ? glowLowHpColor : glowColor;
        c.a *= glowIntensity * k;
        glowImage.color = c;
    }

    /// <summary>HPバーの後ろ(ダメージバーよりさらに奥)に、光のImageを作る。</summary>
    private void BuildGlow()
    {
        if (hpSlider == null) return;

        RectTransform target = (RectTransform)hpSlider.transform;
        Transform parent = target.parent;
        if (parent == null) return;

        GameObject go = new GameObject("HPBarGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = target.anchorMin;
        rt.anchorMax = target.anchorMax;
        rt.pivot = target.pivot;
        rt.anchoredPosition = target.anchoredPosition;
        rt.sizeDelta = target.sizeDelta + new Vector2(glowPadding * 2f, glowPadding * 2f);
        rt.localRotation = target.localRotation;
        rt.localScale = target.localScale;

        // HPSlider と DamageSlider のうち奥にある方より、さらに奥へ差し込む
        int index = target.GetSiblingIndex();
        if (damageSlider != null && damageSlider.transform.parent == parent)
            index = Mathf.Min(index, damageSlider.transform.GetSiblingIndex());
        go.transform.SetSiblingIndex(index);

        go.AddComponent<LayoutElement>().ignoreLayout = true;

        glowImage = go.GetComponent<Image>();
        glowImage.sprite = GetGlowSprite();
        glowImage.type = Image.Type.Sliced;
        glowImage.raycastTarget = false;
    }

    /// <summary>やわらかい角丸の光を、コードで1枚だけ生成して使い回す(9スライス用)。</summary>
    private static Sprite GetGlowSprite()
    {
        if (glowSprite != null) return glowSprite;

        const int size = 64;
        const int border = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float min = border, max = size - border;
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(min - (x + 0.5f), 0f, (x + 0.5f) - max);
                float dy = Mathf.Max(min - (y + 0.5f), 0f, (y + 0.5f) - max);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d / border);
                a = a * a * (3f - 2f * a);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();

        glowSprite = Sprite.Create(
            tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        return glowSprite;
    }

    private void OnDestroy()
    {
        if (glowImage != null) Destroy(glowImage.gameObject);
    }
    // ===== 追加ここまで =====
}