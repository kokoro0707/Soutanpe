using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// TMP_Textの各文字を個別にランダムに揺らす(ジッター)。
/// 画面やCanvas全体を揺らすのではなく、「ROUND1」の文字そのものがガタガタ震える効果に使う。
///
/// 仕組み:
///   Play()が呼ばれた瞬間の頂点(文字本来の位置)を保存しておき、
///   そこからの相対オフセットとしてPerlinノイズでランダムに揺らす。
///   時間経過で揺れの強さが0まで減衰し、最後は元の位置にきっちり戻す。
///
///   (ポップイン演出で過去に起きた「一度壊れた頂点情報を基準にしてしまい、
///    二度と元に戻せなくなる」問題と同じ罠を避けるため、
///    常に Play() 開始時にキャッシュした「元の頂点」から計算する)
///
/// 使い方:
///   1. 揺らしたいTMP_Text(例: ROUND Text)と同じGameObjectにアタッチし、
///      Textフィールドにその TMP_Text を割り当てる
///   2. 衝撃を与えたいタイミングで Play() を呼ぶ
/// </summary>
public class TMPCharacterShake : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    [Tooltip("揺れの強さ(ピクセル相当)")]
    [SerializeField] private float strength = 6f;

    [Tooltip("1秒あたりの揺れの細かさ。大きいほどガタガタ震える")]
    [SerializeField] private float frequency = 25f;

    [Tooltip("揺れている時間(秒)。この時間で強さが0まで減衰する")]
    [SerializeField, Min(0.01f)] private float duration = 0.25f;

    private Vector3[][] originalVertices;
    private float[] seedX;
    private float[] seedY;
    private Tween tween;
    private float elapsed;

    /// <summary>
    /// 現在表示されている文字を、その場でガタガタ揺らす。
    /// </summary>
    public void Play()
    {
        tween?.Kill();

        if (text == null)
        {
            Debug.LogWarning("[TMPCharacterShake] Text が未設定です。", this);
            return;
        }

        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;

        // 「元の形」を複製して保存(揺れの計算は常にこれを基準にする)
        originalVertices = new Vector3[info.meshInfo.Length][];
        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            originalVertices[m] = (Vector3[])info.meshInfo[m].vertices.Clone();
        }

        int count = info.characterCount;
        seedX = new float[count];
        seedY = new float[count];
        for (int i = 0; i < count; i++)
        {
            // 文字ごとに違う位相にして、全文字が同じタイミングで揺れないようにする
            seedX[i] = Random.Range(0f, 100f);
            seedY[i] = Random.Range(0f, 100f);
        }

        elapsed = 0f;
        tween = DOTween.To(() => elapsed, v => elapsed = v, duration, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true) // Time.timeScale = 0 中でも再生されるように
            .OnUpdate(ApplyShake)
            .OnComplete(ResetVertices);
    }

    private void ApplyShake()
    {
        if (text == null || originalVertices == null) return;

        TMP_TextInfo info = text.textInfo;
        float remaining = Mathf.Clamp01(1f - elapsed / duration);
        float currentStrength = strength * remaining;

        for (int i = 0; i < info.characterCount && i < seedX.Length; i++)
        {
            TMP_CharacterInfo cInfo = info.characterInfo[i];
            if (!cInfo.isVisible) continue;

            int materialIndex = cInfo.materialReferenceIndex;
            int vertexIndex = cInfo.vertexIndex;
            if (materialIndex >= originalVertices.Length) continue;

            Vector3[] baseVerts = originalVertices[materialIndex];
            Vector3[] liveVerts = info.meshInfo[materialIndex].vertices;

            float nx = (Mathf.PerlinNoise(seedX[i], Time.unscaledTime * frequency) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(seedY[i], Time.unscaledTime * frequency) - 0.5f) * 2f;
            Vector3 offset = new Vector3(nx, ny, 0f) * currentStrength;

            for (int v = 0; v < 4; v++)
            {
                liveVerts[vertexIndex + v] = baseVerts[vertexIndex + v] + offset;
            }
        }

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
            text.UpdateGeometry(info.meshInfo[m].mesh, m);
        }
    }

    private void ResetVertices()
    {
        if (text == null || originalVertices == null) return;

        TMP_TextInfo info = text.textInfo;
        for (int m = 0; m < info.meshInfo.Length && m < originalVertices.Length; m++)
        {
            info.meshInfo[m].vertices = (Vector3[])originalVertices[m].Clone();
            info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
            text.UpdateGeometry(info.meshInfo[m].mesh, m);
        }
    }

    private void OnDisable()
    {
        tween?.Kill();
        // 中断された場合でも、文字が歪んだ状態で残らないよう元に戻す
        if (originalVertices != null) ResetVertices();
    }
}