using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// コンボ数を画面に表示する。
///
/// 「自分が与えているコンボ数」を表示したいので、
/// 参照するのは自分ではなく「攻撃を受けている相手側」のFighterHitReceiver。
/// 例: Player1側のコンボ表示には、Player2のFighterHitReceiverを割り当てる。
///
/// 表示ルール:
///   ・1ヒット目から表示する
///   ・ヒットのたびに数字がポップ(拡大→戻る)する
///   ・コンボが途切れても即座に消さず、少し維持してからフェードアウトする
///   ・フェードアウト中に新しいヒットが入ったら、フェードを中断して1から表示し直す
///
/// 注意: 表示/非表示はGameObjectのON/OFFではなく透明度(アルファ)で切り替える。
///       このスクリプトが付いているGameObject自体は常にアクティブのままにしておくこと
///       (非アクティブにするとコルーチンが動かず、イベント購読も解除されてしまうため)。
/// </summary>
public class ComboCounterUI : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("このコンボカウンターが表示する対象。攻撃を受ける側(相手)のFighterHitReceiverを指定する")]
    [SerializeField] private FighterHitReceiver targetReceiver;

    [Header("表示")]
    [SerializeField] private TMP_Text comboText;
    [Tooltip("数字の後ろに付ける文字。数字だけでよければ空欄のままにする")]
    [SerializeField] private string suffix = "";
    [Tooltip("後ろの文字(suffix)の大きさ。数字に対する割合(%)。100で数字と同じ大きさ")]
    [SerializeField, Range(10f, 100f)] private float suffixSizePercent = 50f;
    [Tooltip("ONにすると、後ろの文字の色を下の色にする(OFFなら数字と同じ色)")]
    [SerializeField] private bool useSuffixColor = false;
    [SerializeField] private Color suffixColor = Color.white;
    [Tooltip("後ろの文字の上下位置(em単位)。0で数字の下端揃え、0.5くらいで数字の真ん中あたり、プラスで上へ")]
    [SerializeField] private float suffixVerticalOffset = 0f;
    [Tooltip("数字と後ろの文字の間のすき間(em単位)")]
    [SerializeField] private float suffixSpacing = 0.1f;
    [Tooltip("後ろの文字を数字の下の行に出す")]
    [SerializeField] private bool suffixOnNewLine = false;

    [Header("消えるタイミング")]
    [Tooltip("コンボが途切れてから、表示をそのまま維持する時間(秒)")]
    [SerializeField, Min(0f)] private float holdDuration = 0.6f;
    [Tooltip("維持時間が終わったあと、フェードアウトする時間(秒)")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.4f;

    [Header("コンボ数に応じた拡大")]
    [Tooltip("scaleMaxAtCountヒット時点での最大サイズ倍率(1ヒット目は等倍)")]
    [SerializeField, Min(1f)] private float maxScale = 2.5f;
    [Tooltip("この数のコンボで最大サイズになる")]
    [SerializeField, Min(2)] private int scaleMaxAtCount = 50;

    [Header("ヒット時のポップ演出")]
    [Tooltip("ヒットした瞬間に拡大する倍率")]
    [SerializeField] private float popScale = 1.4f;
    [Tooltip("拡大した状態から通常サイズに戻るまでの時間(秒)")]
    [SerializeField, Min(0.01f)] private float popDuration = 0.15f;

    private Coroutine fadeRoutine;
    private Coroutine popRoutine;

    // KO演出中など、表示を更新させたくないときにtrue
    private bool suppressed;

    private void OnEnable()
    {
        if (targetReceiver != null)
        {
            targetReceiver.OnComboCountChanged += HandleComboCountChanged;
        }

        // 開始時は透明にしておく(GameObjectはアクティブのまま)
        suppressed = false;
        SetAlpha(0f);
        if (comboText != null) comboText.transform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        if (targetReceiver != null)
        {
            targetReceiver.OnComboCountChanged -= HandleComboCountChanged;
        }

        fadeRoutine = null;
        popRoutine = null;
    }

    private void HandleComboCountChanged(int count)
    {
        if (comboText == null) return;

        // KO後などは表示を更新しない
        if (suppressed) return;

        // 非アクティブだとコルーチンを開始できないので、警告だけ出して終了
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                $"[ComboCounterUI] {name} が非アクティブのため表示できません。" +
                "このオブジェクトと親オブジェクトのチェックを入れてアクティブにしてください。",
                this
            );
            return;
        }

        if (count > 0)
        {
            // 攻撃継続中、またはコンボ再スタート
            // → フェード中だったら中断し、現在のcountから表示し直す
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            lastCount = count;
            comboText.text = count + BuildSuffix(1f);
            SetAlpha(1f);

            // コンボ数が増えるほどベースサイズを大きくする(1ヒット=等倍、scaleMaxAtCount=最大)
            float t01 = Mathf.Clamp01((count - 1f) / (scaleMaxAtCount - 1f));
            float baseScale = Mathf.Lerp(1f, maxScale, t01);

            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(PopRoutine(baseScale));
        }
        else
        {
            // コンボ途切れ → 少し維持してからフェードアウト開始
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeOutRoutine());
        }
    }

    /// <summary>
    /// 数字がポンと拡大してから通常サイズへ戻る演出。
    /// </summary>
    private IEnumerator PopRoutine(float baseScale)
    {
        Transform t = comboText.transform;
        float timer = 0f;

        while (timer < popDuration)
        {
            timer += Time.deltaTime;
            float ratio = Mathf.Clamp01(timer / popDuration);
            // ベースサイズ x ポップ倍率 から、ベースサイズへ戻る
            float scale = Mathf.Lerp(baseScale * popScale, baseScale, ratio);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        t.localScale = Vector3.one * baseScale;
        popRoutine = null;
    }

    /// <summary>
    /// holdDuration秒そのまま維持したあと、fadeDuration秒かけてフェードアウトする。
    /// 途中で新しいヒットが入ると、HandleComboCountChanged側でこのコルーチンが停止される。
    /// </summary>
    private IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(holdDuration);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        // GameObjectは非アクティブにせず、透明にするだけにする
        SetAlpha(0f);
        fadeRoutine = null;
    }

    /// <summary>
    /// コンボ表示を即座に消し、以降のコンボ更新も無視する(KO時などに呼ぶ)。
    /// GameObjectは非アクティブにせず、透明にするだけ。
    /// </summary>
    public void Suppress()
    {
        suppressed = true;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (popRoutine != null)
        {
            StopCoroutine(popRoutine);
            popRoutine = null;
        }

        SetAlpha(0f);
        if (comboText != null) comboText.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 後ろの文字(例: "COMBO")を、TMPのリッチテキストで数字より小さくする。
    /// 数字の大きさ・拡大演出はそのまま。
    /// </summary>
    private int lastCount;

    private string BuildSuffix(float alpha)
    {
        if (string.IsNullOrEmpty(suffix)) return "";

        string body = suffix;
        if (useSuffixColor)
        {
            // 色タグを使うとフェードの透明度が効かなくなるので、透明度も色に含める
            Color c = suffixColor;
            c.a *= alpha;
            body = $"<color=#{ColorUtility.ToHtmlStringRGBA(c)}>{body}</color>";
        }

        string sized = $"<size={suffixSizePercent:0}%>{body}</size>";
        if (Mathf.Abs(suffixVerticalOffset) > 0.001f)
            sized = $"<voffset={suffixVerticalOffset:0.##}em>{sized}</voffset>";
        if (!suffixOnNewLine && suffixSpacing > 0.001f)
            sized = $"<space={suffixSpacing:0.##}em>{sized}";
        return suffixOnNewLine ? "\n" + sized : sized;
    }

    private void SetAlpha(float alpha)
    {
        if (comboText == null) return;
        Color c = comboText.color;
        c.a = alpha;
        comboText.color = c;

        // 後ろの文字に色を付けている場合は、その透明度も合わせる
        if (useSuffixColor && lastCount > 0 && !string.IsNullOrEmpty(suffix))
            comboText.text = lastCount + BuildSuffix(alpha);
    }
}