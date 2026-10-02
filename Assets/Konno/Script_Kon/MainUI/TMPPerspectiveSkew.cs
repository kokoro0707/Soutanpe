using TMPro;
using UnityEngine;

/// <summary>
/// TMP_Textの文字を、左から右に向かって少しずつ小さく・高い位置になるように変形させる。
/// 「文字が斜めの面(屋根やベルトなど)に沿って奥へ歪んで載っている」ように見せるための演出。
///
/// 仕組み:
///   Outline/Underlay(縁取り・影)はあくまで「色」を変える機能なので、文字の「形」自体は
///   平面のまま変わらない。斜め奥行き感を出すには、文字の頂点そのものを
///   1文字ずつ少しずつスケール/位置を変えて変形させる必要があるため、このスクリプトで対応する。
///
/// 使い方:
///   1. ROUND Textと同じGameObjectにこのスクリプトをアタッチし、Textフィールドに割り当てる
///   2. 文字を表示/更新した直後に Apply() を呼ぶ
///      (TMPCharacterPopInのPerspective Skewフィールドに割り当てておけば、
///       Play()の中で自動的に呼ばれる)
///
/// 注意:
///   他のスクリプト(TMPCharacterShakeなど)がこの後に text.ForceMeshUpdate() を呼ぶと、
///   TMPが文字のレイアウトを再生成し、ここで加えた変形が消えてリセットされてしまう。
///   そのためTMPCharacterShake側ではForceMeshUpdate()を呼ばないようにしている
///   (Apply()適用後の「今の頂点」をそのまま基準にして揺らすようにする)。
/// </summary>
public class TMPPerspectiveSkew : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    [Tooltip("右端の文字の高さを、左端に対してどれだけ縮めるか(1=縮めない, 0.6=60%の高さ)")]
    [SerializeField, Range(0.1f, 1f)] private float heightScaleAtEnd = 0.6f;

    [Tooltip("右端の文字を、左端に対してどれだけ持ち上げるか(メッシュ単位。フォントサイズに応じて調整)")]
    [SerializeField] private float riseAmount = 30f;

    [Tooltip("右端の文字を、左端に対して横方向にどれだけ詰める/広げるか(0=変化なし)")]
    [SerializeField] private float horizontalShiftAtEnd = 0f;

    [Tooltip("右端の文字ほど、どれだけ傾ける(度)。イタリックのように文字の上側だけ横にずれる(シェア変形)。" +
             "「Sample text」のように、奥へ向かうほど倒れ込んでいくような見た目を作れる")]
    [SerializeField] private float shearDegreesAtEnd = 20f;

    [Tooltip("右端の文字ほど、文字そのものをどれだけ回転させるか(度)。シェアと組み合わせると" +
             "より自然に「倒れて奥へ向かう」感じになる")]
    [SerializeField] private float rotationDegreesAtEnd = 0f;

    /// <summary>
    /// 現在text.textに入っている文字列に対して、斜め変形を適用する。
    /// 文字を差し替えた直後(ForceMeshUpdate済みの状態)で呼ぶこと。
    /// </summary>
    public void Apply()
    {
        if (text == null)
        {
            Debug.LogWarning("[TMPPerspectiveSkew] Text が未設定です。", this);
            return;
        }

        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;
        int count = info.characterCount;
        if (count <= 1) return;

        for (int i = 0; i < count; i++)
        {
            TMP_CharacterInfo cInfo = info.characterInfo[i];
            if (!cInfo.isVisible) continue;

            // t=0(左端の文字) ~ t=1(右端の文字)
            float t = (float)i / (count - 1);
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

            // 回転の中心(文字の中心点)。シェア適用前の形から求める
            Vector3 charCenter = Vector3.zero;
            for (int v = 0; v < 4; v++) charCenter += verts[vertexIndex + v];
            charCenter /= 4f;

            float cosR = Mathf.Cos(rotationRad);
            float sinR = Mathf.Sin(rotationRad);

            for (int v = 0; v < 4; v++)
            {
                Vector3 p = verts[vertexIndex + v];
                p.y = baseY + (p.y - baseY) * scale; // 高さを縮める
                p.x += shearFactor * (p.y - baseY);  // イタリックのようにシェア変形(上側ほど横にずれる)

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