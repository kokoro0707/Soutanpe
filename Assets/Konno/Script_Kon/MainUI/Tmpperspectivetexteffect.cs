using TMPro;
using UnityEngine;

/// <summary>
/// 任意のTMP_Textに、「文字が斜めの面に沿って奥へ傾き・縮みながら続いている」ように見せる
/// 奥行き変形をつける、汎用の単体スクリプト。
///
/// RoundAnnouncementController専用の TMPPerspectiveSkew とは別物で、見出し ロゴ 
/// 注釈など、ゲーム内のどのTMP_Textにもそのまま使い回せるように独立させてある。
/// こちらはTMPCharacterPopInのような特別な組み込みをしなくても、単体でそのまま動く。
///
/// 使い方:
///   1. 効果をつけたいTMP_Text(TextMeshProUGUI)と同じGameObjectにこのスクリプトをアタッチ
///   2. Textフィールドに、そのTMP_Textを割り当てる
///   3. それだけでOK。あとはInspectorの値を好みに調整するだけで効果が反映される
///
/// 仕組み(他のテキストでも安心して使えるようにしている理由):
///   TMPは文字列が変わったりmaxVisibleCharactersが変わったりすると、内部でメッシュを
///   再生成して、手動で加えた頂点変形を消してしまう癖がある。
///   このスクリプトはLateUpdate()で毎フレーム効果を再適用することで、
///   (文字が動的に変わるテキストや、他のスクリプトが同時にメッシュを触るテキストでも)
///   常に最後に奥行き変形が乗った状態を保つ。UIテキスト程度の文字数であれば、
///   毎フレーム計算してもコストはごくわずか。
///   文字列が二度と変わらない静的なテキストなら、Auto Apply Every FrameをOFFにして
///   Apply()を一度だけ呼ぶ運用でも問題ない。
/// </summary>
[DisallowMultipleComponent]
public class TMPPerspectiveTextEffect : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    [Header("変形の強さ")]
    [Tooltip("右端の文字の高さを、左端に対してどれだけ縮めるか(1=縮めない, 0.6=60%の高さ)")]
    [SerializeField, Range(0.1f, 1f)] private float heightScaleAtEnd = 0.6f;

    [Tooltip("右端の文字を、左端に対してどれだけ持ち上げるか(メッシュ単位。フォントサイズに応じて調整)")]
    [SerializeField] private float riseAmount = 20f;

    [Tooltip("右端の文字を、左端に対して横方向にどれだけ詰める/広げるか(0=変化なし)")]
    [SerializeField] private float horizontalShiftAtEnd = 0f;

    [Tooltip("右端の文字ほど、どれだけイタリック的に傾けるか(度)。奥へ倒れ込むような見た目になる")]
    [SerializeField] private float shearDegreesAtEnd = 20f;

    [Tooltip("右端の文字ほど、文字そのものをどれだけ回転させるか(度)")]
    [SerializeField] private float rotationDegreesAtEnd = 0f;

    [Tooltip("ONにすると、変形の向きを逆にする(右端が基準の「左端ほど小さく,傾く」になる)。" +
             "OFF(デフォルト)は今まで通り右端ほど変形が強くなる")]
    [SerializeField] private bool reverseDirection = false;

    [Header("1文字だけの場合")]
    [Tooltip("文字数が1文字だけ(例: ラウンド数字の「1」「2」「3」)の時は、左端/右端という基準が無いため、" +
             "代わりにこの値(0から1)を使って変形の強さを決める。1にすると変形の最大値(Shear/Rotation等の" +
             "AtEnd値)がそのまま適用され、0にすると変形なしになる")]
    [SerializeField, Range(0f, 1f)] private float singleCharacterStrength = 1f;

    [Header("自動適用")]
    [Tooltip("ONの場合、毎フレーム自動で効果を再適用する。" +
             "文字列が動的に変わるテキストにはON推奨。" +
             "起動時に一度だけ表示して以降変わらない静的なテキストなら、OFFにして" +
             "Apply()を手動で一度呼ぶ運用でも構わない")]
    [SerializeField] private bool autoApplyEveryFrame = true;

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        if (autoApplyEveryFrame) Apply();
    }

    /// <summary>
    /// 現在text.textに入っている文字列に対して、奥行き変形を適用する。
    /// Auto Apply Every FrameをOFFにしている場合は、文字を変更した後に
    /// このメソッドを明示的に呼ぶこと。
    /// </summary>
    public void Apply()
    {
        if (text == null)
        {
            Debug.LogWarning("[TMPPerspectiveTextEffect] Text が未設定です。", this);
            return;
        }

        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;
        int count = info.characterCount;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            TMP_CharacterInfo cInfo = info.characterInfo[i];
            if (!cInfo.isVisible) continue;

            // t=0(変形なし側の端) ~ t=1(変形が最大になる側の端)。
            // 文字が1文字だけの場合は左端/右端の基準が無いので、
            // singleCharacterStrengthをそのままtとして使う。
            float t;
            if (count <= 1)
            {
                t = singleCharacterStrength;
            }
            else
            {
                float rawT = (float)i / (count - 1);
                t = reverseDirection ? 1f - rawT : rawT;
            }

            float scale = Mathf.Lerp(1f, heightScaleAtEnd, t);
            float rise = Mathf.Lerp(0f, riseAmount, t);
            float shiftX = Mathf.Lerp(0f, horizontalShiftAtEnd, t);
            float shearFactor = Mathf.Tan(Mathf.Lerp(0f, shearDegreesAtEnd, t) * Mathf.Deg2Rad);
            float rotationRad = Mathf.Lerp(0f, rotationDegreesAtEnd, t) * Mathf.Deg2Rad;

            int materialIndex = cInfo.materialReferenceIndex;
            int vertexIndex = cInfo.vertexIndex;
            if (materialIndex >= info.meshInfo.Length) continue;

            Vector3[] verts = info.meshInfo[materialIndex].vertices;

            // この文字の下端(ベースライン側)を基準にして高さだけ縮める
            float baseY = Mathf.Min(
                verts[vertexIndex + 0].y,
                verts[vertexIndex + 1].y
            );

            // 回転の中心(文字の中心点)
            Vector3 charCenter = Vector3.zero;
            for (int v = 0; v < 4; v++) charCenter += verts[vertexIndex + v];
            charCenter /= 4f;

            float cosR = Mathf.Cos(rotationRad);
            float sinR = Mathf.Sin(rotationRad);

            for (int v = 0; v < 4; v++)
            {
                Vector3 p = verts[vertexIndex + v];
                p.y = baseY + (p.y - baseY) * scale; // 高さを縮める
                p.x += shearFactor * (p.y - baseY);  // イタリックのようにシェア変形

                // 文字の中心を軸にして回転(倒れ込むような見た目)
                float dx = p.x - charCenter.x;
                float dy = p.y - charCenter.y;
                p.x = charCenter.x + dx * cosR - dy * sinR;
                p.y = charCenter.y + dx * sinR + dy * cosR;

                p.y += rise;    // 持ち上げる
                p.x += shiftX;  // 横方向の詰め
                verts[vertexIndex + v] = p;
            }
        }

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
            text.UpdateGeometry(info.meshInfo[m].mesh, m);
        }
    }
}