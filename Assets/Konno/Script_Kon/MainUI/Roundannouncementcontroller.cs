using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// P4U風の「ROUND1」演出を制御する(FIGHTバナーは無し、ROUND表示のみ)。
/// DOTweenで動かしているため、以前のコルーチン版よりスライド/弾み/揺れが滑らかで気持ちいい動きになる。
///
/// バナー本体の見た目(インク飛び散り・太字フォント・斜めの縁取りなど)は
/// Unity側でImage/TMP_Textとして事前に用意しておく(グラフィック素材はこのスクリプトの範囲外)。
/// このスクリプトが担当するのは「動き」だけ:
///   ・画面外から斜めにスライドイン(Ease.OutExpo)
///   ・(TextPopInを設定していれば)スライドインと同時に文字を1文字ずつ出現させる。
///     速度はTMPCharacterPopInのStagger Delayに従う
///   ・着地の瞬間にオーバーシュート(弾む、Ease.OutBack)+画面揺れ(DOShakePosition)
///     +放射状の閃光バースト(着地の衝撃を強調する光のエフェクト)
///   ・少し保持してからフェードアウトで消える(bannerFadeGroupを設定した場合。
///     未設定なら従来通りスライドアウトにフォールバックする)
///
/// 事前準備:
///   DOTween(無料, Asset Store または Package Manager の git URL で導入)が必要。
///   導入後、メニューの Tools > Demigiant > DOTween Utility Panel から
///   「Setup DOTween...」を一度実行しておくこと。
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
    [Tooltip("設定すると、roundTextに直接文字を入れる代わりに1文字ずつポップインさせる。" +
             "未設定なら従来通りroundTextに即座に全文字を表示する")]
    [SerializeField] private TMPCharacterPopIn textPopIn;
    [Tooltip("設定すると、ラウンド数字(例:「1」)だけをROUNDの文字とは別に、" +
             "上から落ちてくる演出で表示する(衝撃演出と同時に発生)。" +
             "未設定なら従来通りroundFormatの{0}に数字を埋め込んでROUNDと一緒に表示する")]
    [SerializeField] private RoundNumberDropIn roundNumberDrop;

    [Header("タイミング")]
    [SerializeField, Min(0.05f)] private float slideInDuration = 0.35f;
    [Tooltip("着地後、オーバーシュートが収まるまでの時間")]
    [SerializeField, Min(0.05f)] private float punchDuration = 0.3f;
    [Tooltip("ROUND表示を保持する時間(秒)")]
    [SerializeField, Min(0f)] private float holdDuration = 0.9f;
    [SerializeField, Min(0.05f)] private float slideOutDuration = 0.25f;
    [SerializeField] private Vector2 slideOutOffset = new Vector2(1400f, 0f);

    [Header("衝撃演出")]
    [Tooltip("バナーが着地した瞬間、一瞬このサイズまで膨らんでから通常に戻る")]
    [SerializeField] private float overshootScale = 1.25f;
    [Tooltip("画面(またはCanvas等)を揺らす対象。未設定なら揺らさない。" +
             "文字だけを揺らしたい場合は、これは未設定にしてTextShakeの方を使う")]
    [SerializeField] private Transform shakeTarget;
    [SerializeField] private float shakeStrength = 6f;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.18f;
    [SerializeField] private int shakeVibrato = 12;
    [SerializeField, Range(0f, 90f)] private float shakeRandomness = 60f;
    [Tooltip("設定すると、画面全体ではなく「ROUND1」の文字自体をガタガタ揺らす(ROUND Textに付けたTMPCharacterShake)")]
    [SerializeField] private TMPCharacterShake textShake;

    [Header("背景フラッシュ(任意)")]
    [Tooltip("バナー登場の瞬間だけパッと光らせる帯や全画面Image。CanvasGroupを付けておく")]
    [SerializeField] private CanvasGroup flashStreak;
    [SerializeField, Min(0.01f)] private float flashFadeOutDuration = 0.2f;

    [Header("放射バースト(着地の瞬間の閃光、任意)")]
    [Tooltip("中心から光が飛び散るような円形/星型のSprite(Image)。未設定ならこの演出は無効")]
    [SerializeField] private RectTransform impactBurst;
    [Tooltip("impactBurst側にあるImage/Graphic(アルファのフェードに使う)")]
    [SerializeField] private Graphic impactBurstGraphic;
    [Tooltip("バーストが始まる瞬間の小さいスケール")]
    [SerializeField] private float burstStartScale = 0.3f;
    [Tooltip("バーストが広がりきった時の大きいスケール")]
    [SerializeField] private float burstEndScale = 1.3f;
    [SerializeField, Min(0.05f)] private float burstDuration = 0.3f;
    [Tooltip("広がりながら少しだけ回転させると勢いが出る(度)")]
    [SerializeField] private float burstSpinAmount = 10f;

    [Header("退場フェード(任意)")]
    [Tooltip("バナー全体(Image+Text)にまとめて付けたCanvasGroup。設定するとスライドアウトの代わりにフェードアウトで消える")]
    [SerializeField] private CanvasGroup bannerFadeGroup;
    [Tooltip("フェードアウト中、少しだけ拡大しながら消える(1=拡大なし)")]
    [SerializeField] private float fadeOutEndScale = 1.1f;

    [Header("SE")]
    [SerializeField] private AudioClip roundSe;

    [Header("完了通知")]
    [Tooltip("ROUND表示が消えたタイミングで呼ぶ(このタイミングでキャラクターの操作を解禁する想定)")]
    [SerializeField] private UnityEvent onFightStart;
    [Tooltip("演出が完全に終わったタイミングで呼ぶ(onFightStartと同時)")]
    [SerializeField] private UnityEvent onSequenceEnd;

    private Sequence sequence;

    /// <summary>
    /// ラウンド演出を再生する。例: Play(1) で「ROUND 1」。
    /// </summary>
    public void Play(int roundNumber)
    {
        Debug.Log($"[RoundAnnouncementController] Play({roundNumber}) が呼ばれました。", this);

        if (!HasBanner(roundBanner))
        {
            Debug.LogWarning(
                "[RoundAnnouncementController] Round Banner > Rect が未設定です。" +
                "Inspectorで「Round Banner」を展開し、Rect に RoundBanner の RectTransform をドラッグしてください。" +
                "これが未設定だとスライド/バウンド/フェードのアニメーションは一切再生されません。",
                this);
        }

        // 連打・多重呼び出し対策: 前の演出が残っていたら破棄してから作り直す
        sequence?.Kill(true);

        gameObject.SetActive(true);

        string numberText = roundNumber.ToString();

        // RoundNumberDropが設定されている場合は、数字をROUND側のテキストに含めない
        // (数字は別演出(上から落ちてくる)で表示するため)。未設定なら従来通りroundFormatの{0}に埋め込む。
        string formattedText = roundNumberDrop != null
            ? roundFormat.Replace("{0}", string.Empty)
            : string.Format(roundFormat, roundNumber);

        // textPopInが未設定の場合だけ、従来通り即座に全文字を表示する
        // (textPopIn使用時は、AppendSlideIn開始と同時にPlay()で1文字ずつ出す)
        if (textPopIn == null && roundText != null)
        {
            roundText.text = formattedText;
        }

        PlaySe(roundSe);

        sequence = DOTween.Sequence();
        // Time.timeScale = 0 で止めている間も再生できるよう、Unscaled Timeで動かす
        sequence.SetUpdate(true);

        AppendFlash(sequence);
        AppendSlideIn(sequence, roundBanner, formattedText);

        // タイプライター(1文字ずつ表示)が、バナーの着地(slideInDuration+punchDuration)より
        // 長くかかってしまう場合だけ、衝撃演出(揺れ・バースト)をその分だけ待たせる。
        // Stagger Delayが適切な値(着地までに全文字出終わる速さ)なら、この待機は0になる。
        if (textPopIn != null)
        {
            float bannerLandTime = slideInDuration + punchDuration;
            float typewriterDuration = textPopIn.GetDuration(formattedText);
            float extraWait = typewriterDuration - bannerLandTime;
            if (extraWait > 0f)
            {
                Debug.LogWarning(
                    $"[RoundAnnouncementController] 文字表示(Stagger Delay={textPopIn.StaggerDelay}s × {formattedText.Length}文字" +
                    $"={typewriterDuration:F2}s)がバナーの着地({bannerLandTime:F2}s)より長いため、" +
                    $"衝撃演出を{extraWait:F2}秒待たせています。気になる場合はTMPCharacterPopInのStagger Delayを" +
                    "小さくするか、SlideInDuration/PunchDurationを長くしてください。",
                    this);
                sequence.AppendInterval(extraWait);
            }
        }

        AppendImpactEffects(sequence, numberText);

        sequence.AppendInterval(holdDuration);

        AppendExit(sequence, roundBanner);

        sequence.AppendCallback(() =>
        {
            // ROUND表示が消えたタイミングで操作解禁の通知を出す
            onFightStart?.Invoke();
            onSequenceEnd?.Invoke();
            gameObject.SetActive(false);
        });
    }

    private static bool HasBanner(BannerPart part) => part != null && part.rect != null;

    /// <summary>
    /// 画面外(startOffset)から endPosition まで、回転しながらスライドインし、
    /// 着地の瞬間にオーバーシュート(膨らみ)を入れる。
    /// </summary>
    private void AppendSlideIn(Sequence seq, BannerPart part, string formattedText)
    {
        if (!HasBanner(part)) return;

        RectTransform rect = part.rect;

        seq.AppendCallback(() =>
        {
            rect.gameObject.SetActive(true);
            rect.localScale = Vector3.one;
            rect.anchoredPosition = part.endPosition + part.startOffset;
            rect.localRotation = Quaternion.Euler(0f, 0f, part.startRotation);

            // Stagger Delay(Inspectorで設定した1文字あたりの間隔)の速度でそのまま再生する。
            // (以前はslideInDurationに強制的に合わせていたが、文字数で割った間隔が短すぎて
            //  1フレームの処理の重さで2文字分が一気に進んでしまい、「RとOが同時に出る」原因になっていた)
            if (textPopIn != null)
            {
                textPopIn.Play(formattedText);
            }
        });

        // スライドと回転を同時に、勢いよく飛び込ませてから最後でスッと止まる(OutExpo)
        seq.Append(rect.DOAnchorPos(part.endPosition, slideInDuration).SetEase(Ease.OutExpo));
        seq.Join(rect.DOLocalRotate(new Vector3(0f, 0f, part.endRotation), slideInDuration).SetEase(Ease.OutExpo));

        // 着地の瞬間に大きく弾んで(OutBack)元のサイズに収まる
        seq.Append(rect.DOScale(overshootScale, punchDuration * 0.35f).SetEase(Ease.OutQuad));
        seq.Append(rect.DOScale(1f, punchDuration * 0.65f).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// バナーの退場。bannerFadeGroup が設定されていればフェードアウト(動画と同じ、溶けるように消える)、
    /// 未設定なら従来通り画面外(slideOutOffset)へスライドアウトする。
    /// </summary>
    private void AppendExit(Sequence seq, BannerPart part)
    {
        if (!HasBanner(part)) return;

        RectTransform rect = part.rect;

        if (bannerFadeGroup != null)
        {
            seq.AppendCallback(() =>
            {
                bannerFadeGroup.alpha = 1f;
                // RoundNumberDropの数字はバナー本体と別物なので、消えずに残ってしまう。
                // バナーの退場と同時にフェードアウトさせる
                roundNumberDrop?.Hide(slideOutDuration);
            });
            seq.Append(bannerFadeGroup.DOFade(0f, slideOutDuration).SetEase(Ease.InQuad));
            seq.Join(rect.DOScale(fadeOutEndScale, slideOutDuration).SetEase(Ease.InQuad));
            seq.AppendCallback(() =>
            {
                rect.gameObject.SetActive(false);
                bannerFadeGroup.alpha = 1f; // 次回の再生に備えて戻す
            });
        }
        else
        {
            Vector2 end = part.endPosition + slideOutOffset;

            seq.AppendCallback(() => roundNumberDrop?.Hide(slideOutDuration));

            // 勢いをつけて画面外へ吹き飛ぶように(InBack: 一瞬タメてから加速)
            seq.Append(rect.DOAnchorPos(end, slideOutDuration).SetEase(Ease.InBack));
            seq.AppendCallback(() => rect.gameObject.SetActive(false));
        }
    }

    /// <summary>
    /// 着地の瞬間にまとめて発生する衝撃演出:
    ///   ・画面(shakeTarget)の揺れ
    ///   ・中心から光が放射状に広がって消えるバースト(動画の「文字が出る瞬間に光の線が飛び散る」表現の簡易版)
    /// 両方を同じタイミングでJoinし、"着地の一撃"として一体感を持たせる。
    /// </summary>
    private void AppendImpactEffects(Sequence seq, string numberText)
    {
        bool hasShake = shakeTarget != null;
        bool hasBurst = impactBurst != null;
        bool hasTextShake = textShake != null;
        bool hasNumberDrop = roundNumberDrop != null;
        if (!hasShake && !hasBurst && !hasTextShake && !hasNumberDrop) return;

        Vector3 originalShakePos = hasShake ? shakeTarget.localPosition : Vector3.zero;

        // 全ての初期化を同じタイミングで行う(このAppendCallbackが"着地の瞬間"の基準点になる)
        seq.AppendCallback(() =>
        {
            if (hasBurst)
            {
                impactBurst.gameObject.SetActive(true);
                impactBurst.localScale = Vector3.one * burstStartScale;
                impactBurst.localRotation = Quaternion.identity;

                if (impactBurstGraphic != null)
                {
                    impactBurstGraphic.DOKill();
                    Color c = impactBurstGraphic.color;
                    c.a = 1f;
                    impactBurstGraphic.color = c;
                }
            }

            if (hasTextShake)
            {
                textShake.Play();
            }

            if (hasNumberDrop)
            {
                roundNumberDrop.Play(numberText);
            }
        });

        if (hasShake)
        {
            seq.Join(
                shakeTarget.DOShakePosition(
                    shakeDuration,
                    strength: shakeStrength,
                    vibrato: shakeVibrato,
                    randomness: shakeRandomness,
                    fadeOut: true
                ).SetEase(Ease.OutQuad)
            );
        }

        if (hasBurst)
        {
            seq.Join(impactBurst.DOScale(burstEndScale, burstDuration).SetEase(Ease.OutCubic));
            seq.Join(impactBurst.DOLocalRotate(new Vector3(0f, 0f, burstSpinAmount), burstDuration).SetEase(Ease.OutCubic));

            if (impactBurstGraphic != null)
            {
                seq.Join(impactBurstGraphic.DOFade(0f, burstDuration).SetEase(Ease.OutCubic));
            }
        }

        seq.AppendCallback(() =>
        {
            if (hasShake) shakeTarget.localPosition = originalShakePos;
            if (hasBurst) impactBurst.gameObject.SetActive(false);
        });
    }

    /// <summary>
    /// バナー登場の瞬間、背景の帯やCanvas全体をパッと光らせてすぐ消す。
    /// </summary>
    private void AppendFlash(Sequence seq)
    {
        if (flashStreak == null) return;

        seq.AppendCallback(() =>
        {
            flashStreak.gameObject.SetActive(true);
            flashStreak.alpha = 1f;
        });

        seq.Append(flashStreak.DOFade(0f, flashFadeOutDuration).SetEase(Ease.OutQuad));
        seq.AppendCallback(() => flashStreak.gameObject.SetActive(false));
    }

    private void PlaySe(AudioClip clip)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }

    private void OnDisable()
    {
        // シーン遷移やオブジェクト破棄時にTweenが残らないようにする
        sequence?.Kill();
    }
}