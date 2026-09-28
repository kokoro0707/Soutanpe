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

    [Header("消えるタイミング")]
    [Tooltip("コンボが途切れてから、表示をそのまま維持する時間(秒)")]
    [SerializeField, Min(0f)] private float holdDuration = 0.6f;
    [Tooltip("維持時間が終わったあと、フェードアウトする時間(秒)")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.4f;

    [Header("ヒット時のポップ演出")]
    [Tooltip("ヒットした瞬間に拡大する倍率")]
    [SerializeField] private float popScale = 1.4f;
    [Tooltip("拡大した状態から通常サイズに戻るまでの時間(秒)")]
    [SerializeField, Min(0.01f)] private float popDuration = 0.15f;

    private Coroutine fadeRoutine;
    private Coroutine popRoutine;

    private void OnEnable()
    {
        if (targetReceiver != null)
        {
            targetReceiver.OnComboCountChanged += HandleComboCountChanged;
        }

        // 開始時は透明にしておく(GameObjectはアクティブのまま)
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

            comboText.text = count + suffix;
            SetAlpha(1f);

            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(PopRoutine());
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
    private IEnumerator PopRoutine()
    {
        Transform t = comboText.transform;
        float timer = 0f;

        while (timer < popDuration)
        {
            timer += Time.deltaTime;
            float ratio = Mathf.Clamp01(timer / popDuration);
            float scale = Mathf.Lerp(popScale, 1f, ratio);
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        t.localScale = Vector3.one;
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

    private void SetAlpha(float alpha)
    {
        if (comboText == null) return;
        Color c = comboText.color;
        c.a = alpha;
        comboText.color = c;
    }
}