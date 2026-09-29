using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// P4U風の「ROUND1」演出を制御する(FIGHTバナーは無し、ROUND表示のみ)。
///
/// バナー本体の見た目(インク飛び散り・太字フォント・斜めの縁取りなど)は
/// Unity側でImage/TMP_Textとして事前に用意しておく(グラフィック素材はこのスクリプトの範囲外)。
/// このスクリプトが担当するのは「動き」だけ:
///   ・画面外から斜めにスライドイン
///   ・着地の瞬間にオーバーシュート(弾む)+画面揺れ
///   ・少し保持してからスライドアウト
///
/// Hierarchy例:
///   RoundAnnouncement (このスクリプトをアタッチ、最初はGameObjectごと非アクティブでOK)
///    └─ RoundBanner (ROUND1の見た目、RectTransform)
///        └─ RoundText (TMP_Text、中に"ROUND{0}"を書き込む)
/// </summary>
public class RoundAnnouncementController : MonoBehaviour
{
    [System.Serializable]
    public class BannerPart
    {
        [Tooltip("動かす対象のRectTransform(バナー本体)")]
        public RectTransform rect;
        [Tooltip("画面外の開始位置からのオフセット(anchoredPosition基準)。ここから中央へ飛び込んでくる")]
        public Vector2 startOffset = new Vector2(-1400f, 0f);
        [Tooltip("最終的に止まる位置(anchoredPosition)")]
        public Vector2 endPosition = Vector2.zero;
        [Tooltip("飛び込んでくる際の開始回転(度)")]
        public float startRotation = -25f;
        [Tooltip("最終的な回転(度)")]
        public float endRotation = -12f;
    }

    [Header("ラウンドバナー(例: ROUND1)")]
    [SerializeField] private BannerPart roundBanner;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private string roundFormat = "ROUND{0}";

    [Header("タイミング")]
    [SerializeField, Min(0.05f)] private float slideInDuration = 0.25f;
    [Tooltip("ROUND表示を保持する時間(秒)")]
    [SerializeField, Min(0f)] private float holdDuration = 0.9f;
    [SerializeField, Min(0.05f)] private float slideOutDuration = 0.2f;
    [SerializeField] private Vector2 slideOutOffset = new Vector2(1400f, 0f);

    [Header("衝撃演出")]
    [Tooltip("バナーが着地した瞬間、一瞬このサイズまで膨らんでから通常に戻る")]
    [SerializeField] private float overshootScale = 1.15f;
    [Tooltip("画面(またはCanvas等)を揺らす対象。未設定なら揺らさない")]
    [SerializeField] private Transform shakeTarget;
    [SerializeField] private float shakeStrength = 12f;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.15f;

    [Header("背景フラッシュ(任意)")]
    [Tooltip("バナー登場の瞬間だけパッと光らせる帯や全画面Image。CanvasGroupを付けておく")]
    [SerializeField] private CanvasGroup flashStreak;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.15f;

    [Header("SE")]
    [SerializeField] private AudioClip roundSe;

    [Header("完了通知")]
    [Tooltip("ROUND表示が消えたタイミングで呼ぶ(このタイミングでキャラクターの操作を解禁する想定)")]
    [SerializeField] private UnityEvent onFightStart;
    [Tooltip("演出が完全に終わったタイミングで呼ぶ(onFightStartと同時)")]
    [SerializeField] private UnityEvent onSequenceEnd;

    /// <summary>
    /// ラウンド演出を再生する。例: Play(1) で「ROUND 1」。
    /// </summary>
    public void Play(int roundNumber)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(SequenceRoutine(roundNumber));
    }

    private IEnumerator SequenceRoutine(int roundNumber)
    {
        if (roundText != null)
        {
            roundText.text = string.Format(roundFormat, roundNumber);
        }

        PlaySe(roundSe);

        yield return FlashRoutine();

        yield return SlideIn(roundBanner);
        Shake();

        yield return new WaitForSecondsRealtime(holdDuration);

        yield return SlideOut(roundBanner);

        // ROUND表示が消えたタイミングで操作解禁の通知を出す
        onFightStart?.Invoke();
        onSequenceEnd?.Invoke();

        gameObject.SetActive(false);
    }

    private static bool HasBanner(BannerPart part) => part != null && part.rect != null;

    /// <summary>
    /// 画面外(startOffset)から endPosition まで、回転しながらスライドインし、
    /// 着地の瞬間にオーバーシュート(膨らみ)を入れる。
    /// </summary>
    private IEnumerator SlideIn(BannerPart part)
    {
        if (!HasBanner(part)) yield break;

        RectTransform rect = part.rect;
        rect.gameObject.SetActive(true);
        rect.localScale = Vector3.one;

        Vector2 start = part.endPosition + part.startOffset;
        rect.anchoredPosition = start;
        rect.localRotation = Quaternion.Euler(0f, 0f, part.startRotation);

        float t = 0f;
        while (t < slideInDuration)
        {
            t += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(t / slideInDuration);
            float eased = 1f - Mathf.Pow(1f - ratio, 3f); // ease-out cubic

            rect.anchoredPosition = Vector2.Lerp(start, part.endPosition, eased);
            rect.localRotation = Quaternion.Euler(
                0f, 0f,
                Mathf.Lerp(part.startRotation, part.endRotation, eased)
            );

            yield return null;
        }

        rect.anchoredPosition = part.endPosition;
        rect.localRotation = Quaternion.Euler(0f, 0f, part.endRotation);

        yield return PunchScale(rect, overshootScale);
    }

    private IEnumerator PunchScale(RectTransform rect, float fromScale)
    {
        float duration = Mathf.Max(0.05f, slideInDuration * 0.6f);
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - ratio, 3f);
            float scale = Mathf.Lerp(fromScale, 1f, eased);
            rect.localScale = Vector3.one * scale;
            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    /// <summary>
    /// endPosition から画面外(slideOutOffset)へスライドアウトして非表示にする。
    /// </summary>
    private IEnumerator SlideOut(BannerPart part)
    {
        if (!HasBanner(part)) yield break;

        RectTransform rect = part.rect;
        Vector2 start = rect.anchoredPosition;
        Vector2 end = part.endPosition + slideOutOffset;

        float t = 0f;
        while (t < slideOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(t / slideOutDuration);
            float eased = ratio * ratio; // ease-in
            rect.anchoredPosition = Vector2.Lerp(start, end, eased);
            yield return null;
        }

        rect.anchoredPosition = end;
        rect.gameObject.SetActive(false);
    }

    /// <summary>
    /// バナー登場の瞬間、背景の帯やCanvas全体をパッと光らせてすぐ消す。
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        if (flashStreak == null) yield break;

        flashStreak.gameObject.SetActive(true);
        flashStreak.alpha = 1f;

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            flashStreak.alpha = Mathf.Lerp(1f, 0f, t / flashDuration);
            yield return null;
        }

        flashStreak.alpha = 0f;
        flashStreak.gameObject.SetActive(false);
    }

    private void Shake()
    {
        if (shakeTarget != null) StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 original = shakeTarget.localPosition;
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float remaining = 1f - Mathf.Clamp01(t / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * shakeStrength * remaining;
            shakeTarget.localPosition = original + (Vector3)offset;
            yield return null;
        }

        shakeTarget.localPosition = original;
    }

    private void PlaySe(AudioClip clip)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }
}