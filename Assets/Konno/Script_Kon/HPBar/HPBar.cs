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

    private float timer;

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

        if (damageSlider == null) return;

        if (damageSlider.value <= ratio)
        {
            // 回復時、またはダメージバーがすでに追いついている場合は即座に同期
            damageSlider.SetValueWithoutNotify(ratio);
            timer = 0f;
        }
        // ダメージでバーが減った場合は、Update側で delay 後にゆっくり追従させる
    }

    private void Update()
    {
        // 呼び出しタイミングのズレや参照の初期化順に関係なく、
        // 毎フレーム強制的に正しい色へ合わせる(黒残り対策)
        if (changeColorWhenLow && hpFillImage != null && hpSlider != null)
        {
            Color targetColor =
                hpSlider.value <= lowHpThreshold ? lowHpColor : normalColor;

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
}