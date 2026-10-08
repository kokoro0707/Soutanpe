using UnityEngine;
using UnityEngine.UI;

namespace PersonaMenuUI
{
    /// <summary>
    /// カーソルの「後ろ」に重ねる、赤いアクセント(ずれた赤い帯)。
    /// カーソルの左上へ少しはみ出して見え、カーソルが動くと少し遅れて追従する。
    ///
    /// 使い方:
    ///   1. 赤い SlantedRect(または Image)を1つ作る。色は赤(例: #E8212B)
    ///   2. Hierarchy上で、カーソルより「上(手前ではなく奥)」に並べる = カーソルより前に置く
    ///      ※ カーソルのMaskの「中」ではなく、外側に置くこと(中に入れるとはみ出し部分が切れる)
    ///   3. この赤い帯にこのコンポーネントを付け、Follow に カーソル(CursorRect)を入れる
    ///   4. 項目ごとにカーソルが別々の場合(Item Cursors)は、Follow Targets に全部入れる。
    ///      その時は「いま表示されているカーソル」に自動で追従する
    /// </summary>
    [DisallowMultipleComponent]
    public class CursorAccent : MonoBehaviour
    {
        [Header("追従先")]
        [Tooltip("カーソル1つの場合はこれだけでOK")]
        [SerializeField] private RectTransform follow;
        [Tooltip("項目ごとにカーソルが別々の場合は、ここに全部入れる(表示中のものに追従)")]
        [SerializeField] private RectTransform[] followTargets;

        [Header("ずらし・大きさ")]
        [Tooltip("カーソルからのずれ(ピクセル)。左上にはみ出させるなら X=マイナス, Y=プラス")]
        [SerializeField] private Vector2 offset = new Vector2(-10f, 8f);
        [Tooltip("カーソルより横・縦にどれだけ大きくするか(ピクセル)。右端にもはみ出させたい時は横を大きめに")]
        [SerializeField] private Vector2 extraSize = new Vector2(30f, 6f);

        [Header("動き")]
        [Tooltip("追従の遅れ。0=ぴったり、大きいほど素早く追いつく(8〜20くらい)。小さいと、ゆったり遅れて付いてくる")]
        [SerializeField, Min(0f)] private float followSpeed = 14f;
        [Tooltip("ONにすると、ポーズ中(timeScale=0)でも動く")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("斜め(SlantedRect)との連携")]
        [Tooltip("ONにすると、追従先のSlantedRectと同じ傾き(Skew Angle)にそろえる。" +
                 "SlantedRectは上辺だけが横にずれる形なので、そろえないと斜めがバラバラに見える")]
        [SerializeField] private bool matchSkew = true;
        [Tooltip("そろえた傾きに足す角度(度)。0=完全に同じ。少し変えると、ずれた影のように見える")]
        [SerializeField] private float extraSkewAngle = 0f;

        [Header("見た目")]
        [Tooltip("ONにすると、カーソルの傾き(回転)も合わせる")]
        [SerializeField] private bool matchRotation = true;
        [Tooltip("ONにすると、追従先が非表示の間はこの赤い帯も隠す")]
        [SerializeField] private bool hideWhenTargetHidden = true;

        private RectTransform rt;
        private Graphic graphic;
        private SlantedRect accentSlanted;
        private bool initialized;

        private void Awake()
        {
            rt = (RectTransform)transform;
            graphic = GetComponent<Graphic>();
            accentSlanted = GetComponent<SlantedRect>();

            // 大きさをsizeDeltaで素直に扱えるよう、アンカーと軸を中央にそろえる
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private void OnEnable()
        {
            initialized = false;
        }

        private void LateUpdate()
        {
            RectTransform target = ResolveTarget();

            if (target == null)
            {
                if (hideWhenTargetHidden && graphic != null) graphic.enabled = false;
                return;
            }
            if (graphic != null) graphic.enabled = true;

            // 大きさ: ターゲットの見た目の大きさ(スケール込み)を、自分のスケールで割って合わせる
            Vector3 ts = target.lossyScale, ms = rt.lossyScale;
            float sx = ms.x != 0f ? ts.x / ms.x : 1f;
            float sy = ms.y != 0f ? ts.y / ms.y : 1f;
            Vector2 targetSize = new Vector2(
                target.rect.width * sx + extraSize.x,
                target.rect.height * sy + extraSize.y);

            // SlantedRectは「下辺が左端に固定で、上辺だけ横にずれる」平行四辺形。
            // 見た目の中心は、矩形の中心から (高さ×tan/2) だけ右にずれる。
            // カーソルと赤い帯で傾き・高さが違うと、このずれ方も違ってしまうので、
            // 見た目の中心で位置をそろえる。
            SlantedRect targetSlanted = target.GetComponent<SlantedRect>();
            if (targetSlanted == null) targetSlanted = target.GetComponentInChildren<SlantedRect>();

            float shiftX = 0f; // ターゲット側の座標系での補正量
            if (accentSlanted != null)
            {
                if (matchSkew && targetSlanted != null)
                {
                    float wanted = targetSlanted.SkewAngle + extraSkewAngle;
                    if (!Mathf.Approximately(accentSlanted.SkewAngle, wanted))
                        accentSlanted.SkewAngle = wanted;
                }

                float tanT = targetSlanted != null ? Mathf.Tan(Mathf.Deg2Rad * targetSlanted.SkewAngle) : 0f;
                float tanA = Mathf.Tan(Mathf.Deg2Rad * accentSlanted.SkewAngle);
                float accentHeightInTarget = sy != 0f ? targetSize.y / sy : targetSize.y;
                shiftX = tanT * target.rect.height * 0.5f - tanA * accentHeightInTarget * 0.5f;
            }

            // ワールド座標でそろえるので、親が違っても動く
            Vector3 targetPos = target.TransformPoint(
                new Vector3(
                    (0.5f - target.pivot.x) * target.rect.width + offset.x + shiftX,
                    (0.5f - target.pivot.y) * target.rect.height + offset.y,
                    0f));
            Quaternion targetRot = matchRotation ? target.rotation : rt.rotation;

            if (!initialized || followSpeed <= 0f)
            {
                rt.position = targetPos;
                rt.rotation = targetRot;
                rt.sizeDelta = targetSize;
                initialized = true;
                return;
            }

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = 1f - Mathf.Exp(-followSpeed * dt); // フレームレートに依存しない追従
            rt.position = Vector3.Lerp(rt.position, targetPos, k);
            rt.rotation = Quaternion.Slerp(rt.rotation, targetRot, k);
            rt.sizeDelta = Vector2.Lerp(rt.sizeDelta, targetSize, k);
        }

        /// <summary>表示されているカーソルを選ぶ。</summary>
        private RectTransform ResolveTarget()
        {
            if (followTargets != null && followTargets.Length > 0)
            {
                foreach (RectTransform t in followTargets)
                {
                    if (t == null || !t.gameObject.activeInHierarchy) continue;
                    if (t.lossyScale.x < 0.01f) continue; // スケール0で隠している場合
                    return t;
                }
                return null;
            }

            if (follow != null && follow.gameObject.activeInHierarchy) return follow;
            return null;
        }
    }
}