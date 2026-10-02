using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// ラウンド数字(例: 「1」)だけを、画面の上から大きく勢いよく落ちてきて
/// 着地の瞬間にガツン!と収まるように表示する。
/// 「ROUND」の文字が1文字ずつ出現する演出(TMPCharacterPopIn)とは完全に別の、
/// もう一枚上に乗った演出として動かす。
///
/// 使い方:
///   1. 数字("1"など)を表示するTMP_Text(TextMeshProUGUI)を、ROUND1バナーの中に
///      最終的に収まる位置に配置しておく(このRectTransformの位置が「着地点」になる)
///   2. そのGameObjectにこのスクリプトをアタッチし、Rect/Textフィールドに割り当てる
///   3. 出したいタイミングで Play("1") を呼ぶ
///      (RoundAnnouncementControllerにRoundNumberDropフィールドを割り当てておけば、
///       衝撃演出(揺れ・バースト)と同時に自動で呼ばれる)
/// </summary>
public class RoundNumberDropIn : MonoBehaviour
{
    [SerializeField] private RectTransform rect;
    [SerializeField] private TMP_Text text;

    [Tooltip("着地点から見て、どれだけ上の位置からスタートするか(anchoredPosition基準)")]
    [SerializeField] private Vector2 dropFromOffset = new Vector2(0f, 500f);

    [Tooltip("落ちてくる最中の拡大サイズ(大きい数字がズドンと落ちてくる感じを出す)")]
    [SerializeField] private float startScale = 1.8f;

    [Tooltip("落下にかかる時間(秒)。動画のように短く・速くすると勢いが出る")]
    [SerializeField, Min(0.01f)] private float dropDuration = 0.12f;

    [Tooltip("着地の瞬間、一度だけ膨らむ大きさ(弾みの強さ)")]
    [SerializeField] private float overshootScale = 1.15f;

    [Tooltip("着地後の弾みが収まって通常サイズに戻るまでの時間")]
    [SerializeField, Min(0.01f)] private float settleDuration = 0.12f;

    private Vector2 endPosition;
    private bool endPositionCaptured;
    private Sequence sequence;

    /// <summary>
    /// 数字を差し替えて、上から落ちてくる演出を再生する。
    /// </summary>
    public void Play(string number)
    {
        sequence?.Kill();

        if (rect == null || text == null)
        {
            Debug.LogWarning("[RoundNumberDropIn] Rect または Text が未設定です。", this);
            return;
        }

        // 最初にPlay()が呼ばれた時点のRectTransformの位置を「着地点」として記憶しておく。
        // (Inspectorで事前に最終表示位置に置いておく想定)
        if (!endPositionCaptured)
        {
            endPosition = rect.anchoredPosition;
            endPositionCaptured = true;
        }

        text.text = number;

        rect.gameObject.SetActive(true);
        rect.anchoredPosition = endPosition + dropFromOffset;
        rect.localScale = Vector3.one * startScale;

        sequence = DOTween.Sequence();
        sequence.SetUpdate(true); // Time.timeScale = 0 中でも再生されるように

        // 上から勢いよく落ちてくる(着地直前まで速度を保ったまま突っ込む: Ease.InExpo)
        sequence.Append(rect.DOAnchorPos(endPosition, dropDuration).SetEase(Ease.InExpo));
        sequence.Join(rect.DOScale(overshootScale, dropDuration).SetEase(Ease.InExpo));

        // 着地の瞬間、一度膨らんでから通常サイズに収まる(ガツン!という弾み)
        sequence.Append(rect.DOScale(1f, settleDuration).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// 表示中の数字をフェードアウトして消す(バナー本体の退場と合わせて呼ぶ想定)。
    /// 呼ばないと、バナーが退場しても数字だけその場に残ってしまう。
    /// </summary>
    public void Hide(float fadeDuration)
    {
        sequence?.Kill();

        if (rect == null) return;

        if (text != null)
        {
            text.DOKill();
            text.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    rect.gameObject.SetActive(false);
                    // 次回のPlay()で即表示できるよう、アルファを元に戻しておく
                    Color c = text.color;
                    c.a = 1f;
                    text.color = c;
                });
        }
        else
        {
            rect.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        sequence?.Kill();
    }
}