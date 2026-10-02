using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// TMP_Textを1文字ずつ出現させる(タイプライター表示)。
/// 「R」→「RO」→「ROU」…のように、少し時間差をつけて文字が次々に出現していく演出に使う
/// (P4U風ROUND1バナーの文字出現用)。
///
/// 仕組み:
///   TMP_Textの標準機能 maxVisibleCharacters を使う。
///   (文字の頂点を直接書き換えて拡大/フェードさせるやり方は、TMPの内部メッシュ再構築タイミングと
///    競合して反映されないことがあったため、公式にサポートされているこの方式に変更した)
///
/// 使い方:
///   1. 表示したいTMP_Text(TextMeshProUGUI)と同じGameObjectに、このスクリプトをアタッチし、
///      Textフィールドにその TMP_Text を割り当てる
///   2a. Play("ROUND1") … Stagger Delay(Inspector)の間隔で1文字ずつ出す(単体で使う場合)
///   2b. Play("ROUND1", 0.35f) … 指定した秒数(例: バナーのスライド時間)に収まるよう、
///       文字間隔を自動計算して1文字ずつ出す(バナーの動きと重ねたい場合はこちらを使う)
/// </summary>
public class TMPCharacterPopIn : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    [Tooltip("1文字出現するごとの時間差(秒)。小さいほど畳み掛けるように速く出る。" +
             "Play(string, float)で呼ぶ場合はこの値は使われない(指定した時間に合わせて自動計算される)")]
    [SerializeField, Min(0.001f)] private float staggerDelay = 0.045f;

    [Tooltip("設定すると、文字を表示した直後に斜め奥行き変形(TMPPerspectiveSkew)を適用する")]
    [SerializeField] private TMPPerspectiveSkew perspectiveSkew;

    private Sequence sequence;
    private int visibleCount;

    /// <summary>Inspectorで設定されている1文字あたりの間隔(秒)。</summary>
    public float StaggerDelay => staggerDelay;

    /// <summary>
    /// 指定した文字列を、Stagger Delay(Inspector設定値)の速度で1文字ずつ出現させた場合に
    /// 何秒かかるかを返す(実際には再生せず、時間の計算だけ行う)。
    /// RoundAnnouncementController側で「タイプライターが終わるまで衝撃演出を待つ必要があるか」を
    /// 判断するために使う。
    /// </summary>
    public float GetDuration(string forText)
    {
        if (string.IsNullOrEmpty(forText)) return 0f;
        return forText.Length * staggerDelay;
    }

    /// <summary>
    /// テキストを差し替えて、1文字ずつ出現(タイプライター表示)を再生する。
    /// Stagger Delay(Inspector設定値)に基づいて速度が決まる。
    /// </summary>
    public void Play(string newText)
    {
        PlayInternal(newText, null);
    }

    /// <summary>
    /// テキストを差し替えて、1文字ずつ出現を再生する。
    /// totalDuration秒で全文字が出現し終わるよう、文字間隔を自動的に調整する。
    /// (例: バナーのスライドイン時間と同じ値を渡せば、飛び込んでくる間に文字が組み立つ)
    /// </summary>
    public void Play(string newText, float totalDuration)
    {
        PlayInternal(newText, totalDuration);
    }

    private void PlayInternal(string newText, float? overrideDuration)
    {
        sequence?.Kill();

        if (text == null)
        {
            Debug.LogWarning("[TMPCharacterPopIn] Text が未設定です。", this);
            return;
        }

        text.text = newText;
        text.ForceMeshUpdate();

        // 斜め奥行き変形は、文字の中身を確定させた直後(ここ)で一度だけ適用する。
        // これより後でForceMeshUpdate()を呼ぶ処理があると変形が消えてしまうので注意。
        perspectiveSkew?.Apply();

        int totalCount = text.textInfo.characterCount;

        visibleCount = 0;
        text.maxVisibleCharacters = 0;

        if (totalCount <= 0) return;

        float totalDuration = overrideDuration.HasValue && overrideDuration.Value > 0f
            ? overrideDuration.Value
            : totalCount * staggerDelay;

        sequence = DOTween.Sequence();
        sequence.SetUpdate(true); // Time.timeScale = 0 中でも再生されるように

        sequence.Append(
            DOTween.To(
                () => visibleCount,
                v =>
                {
                    visibleCount = v;
                    text.maxVisibleCharacters = v;

                    // maxVisibleCharactersを変更すると、TMPが内部でメッシュを再生成してしまい、
                    // 最初に一度だけ適用した斜め奥行き変形が毎フレーム消されてしまう。
                    // そのため、表示文字数が変わるたびに(=毎フレーム)変形を再適用し直す。
                    perspectiveSkew?.Apply();
                },
                totalCount,
                totalDuration
            ).SetEase(Ease.Linear)
        );
    }

    private void OnDisable()
    {
        sequence?.Kill();
    }
}