using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// タイトル文字を画面外から指定位置まで勢いよく流し込み、
/// 着いた瞬間に「急ブレーキ」(前のめり→揺り戻し・つぶれ・画面揺れ)を再生する。
/// シーン上で置いた位置が「止まる位置」になる。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TitleSlideIn : MonoBehaviour
{
    [Header("開始")]
    [Tooltip("指定すると、画面展開が終わってから開始する")]
    [SerializeField] private TitleOpenReveal waitForReveal;
    [Tooltip("指定すると、別の TitleSlideIn の後から開始する(例: タイトルの後にサブ文字)")]
    [SerializeField] private TitleSlideIn waitForSlide;
    [Tooltip("ON: 相手が止まる位置に着いた瞬間に開始 / OFF: 相手の揺れ戻しまで全部終わってから開始")]
    [SerializeField] private bool startWhenOtherArrives = true;
    [SerializeField] private float startDelay = 0.1f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool useUnscaledTime = true;

    public enum DirectionMode
    {
        Offset,         // Start Offset をそのまま使う(従来どおり)
        AlongTextAngle, // テキストの回転(Z)に沿って斜めに流す
        CustomAngle,    // Move Angle で指定した角度で斜めに流す
        Preset          // Move To で進む向きを選ぶ(左斜め下へ など)
    }

    public enum MoveTo
    {
        Right,      // 左から来て右へ進む
        Left,       // 右から来て左へ進む
        Up,
        Down,
        UpRight,    // 右斜め上へ
        UpLeft,     // 左斜め上へ
        DownRight,  // 右斜め下へ
        DownLeft    // 左斜め下へ(右上から来て左下へ下がる)
    }

    [Header("移動(画面外 → 指定位置)")]
    [SerializeField] private DirectionMode directionMode = DirectionMode.Offset;
    [Tooltip("AlongTextAngle / CustomAngle 用:止まる位置からスタート地点までの距離")]
    [SerializeField] private float startDistance = 2200f;
    [Tooltip("AlongTextAngle / CustomAngle 用:ONで右(上)側から、OFFで左(下)側から来る")]
    [SerializeField] private bool comeFromRight = false;
    [Tooltip("CustomAngle 用:進む線の角度(0=水平、30=右上がり30°)")]
    [SerializeField] private float moveAngle = 25f;
    [Tooltip("Preset 用:進む向き")]
    [SerializeField] private MoveTo moveTo = MoveTo.DownLeft;
    [Tooltip("Preset の斜め用:水平からの角度(境界線の傾きに合わせる)")]
    [SerializeField, Range(0f, 89f)] private float presetSlantAngle = 20f;
    [Tooltip("止まる位置からどれだけ離れた所からスタートするか。左から来るならXをマイナス")]
    [SerializeField] private Vector2 startOffset = new Vector2(-2200f, 0f);
    [SerializeField] private float moveDuration = 0.32f;
    [Tooltip("1より大きいほど、だんだん加速する")]
    [SerializeField, Range(1f, 4f)] private float accelPower = 1.6f;
    [SerializeField] private float moveStretchX = 1.25f;
    [SerializeField] private float moveSquashY = 0.9f;
    [Tooltip("走行中に後ろへ傾く角度")]
    [SerializeField] private float moveLeanAngle = 10f;

    [Header("急ブレーキ")]
    [Tooltip("止まる位置を通り過ぎる距離")]
    [SerializeField] private float overshoot = 60f;
    [SerializeField] private float brakeDuration = 0.09f;
    [Tooltip("ブレーキで前のめりになる角度")]
    [SerializeField] private float brakeLeanAngle = 22f;
    [SerializeField] private float brakeSquashX = 0.86f;
    [SerializeField] private float brakeStretchY = 1.12f;
    [Tooltip("揺り戻しが収まるまでの時間")]
    [SerializeField] private float settleDuration = 0.55f;
    [Tooltip("揺り戻しの回数(大きいほどプルプルする)")]
    [SerializeField] private float settleFrequency = 2.5f;
    [SerializeField] private float settleDamping = 5f;

    [Header("画面揺れ(任意)")]
    [Tooltip("揺らしたいパネル(タイトル文字の親など)。このオブジェクト自身は指定しない")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private float shakeAmount = 10f;
    [SerializeField] private float shakeDuration = 0.18f;

    [Header("残像")]
    [SerializeField] private bool useAfterImages = true;
    [SerializeField, Range(1, 8)] private int afterImageCount = 4;
    [Tooltip("残像同士の間隔(フレーム数)")]
    [SerializeField, Range(1, 6)] private int afterImageFrameGap = 2;
    [SerializeField, Range(0f, 1f)] private float afterImageAlpha = 0.45f;
    [SerializeField] private float afterImageFadeTime = 0.2f;

    [Header("効果音(任意)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip whooshClip;
    [SerializeField] private AudioClip brakeClip;

    public bool IsFinished { get; private set; }
    public bool HasArrived { get; private set; }
    public event Action OnArrived;

    private RectTransform rt;
    private TMP_Text tmp;
    private TMPPerspectiveTextEffect perspective; // 斜め奥行きテキスト(付いていれば連携)
    private Graphic graphic;
    private Vector2 endPos;
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private float dir = 1f;
    private Vector2 offsetVec;   // 実際に使うスタート地点のずれ
    private Vector2 moveDir;     // 進む向き(親の座標系・正規化)
    private bool shearing;

    private readonly List<Vector2> trail = new List<Vector2>();
    private readonly List<Graphic> ghosts = new List<Graphic>();

    private float Dt => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void Awake()
    {
        rt = (RectTransform)transform;
        tmp = GetComponent<TMP_Text>();
        perspective = GetComponent<TMPPerspectiveTextEffect>();
        graphic = GetComponent<Graphic>();
        endPos = rt.anchoredPosition;
        baseScale = rt.localScale;
        baseRotation = rt.localRotation;
        offsetVec = CalcOffset();
        moveDir = offsetVec.sqrMagnitude > 0.0001f ? -offsetVec.normalized : Vector2.right;
        // テキスト自身の向きで見て右へ進むなら +1(傾き・伸びの向きに使う)
        Vector3 localDir = Quaternion.Inverse(rt.localRotation) * (Vector3)moveDir;
        dir = localDir.x >= 0f ? 1f : -1f;

        if (playOnStart)
            rt.anchoredPosition = endPos + offsetVec; // 最初は画面外に隠す
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        StopAllCoroutines();
        ClearGhosts();
        IsFinished = false;
        HasArrived = false;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        Vector2 startPos = endPos + offsetVec;
        rt.anchoredPosition = startPos;
        ApplyPose(1f, 1f, 0f);

        if (waitForReveal != null)
            while (!waitForReveal.IsFinished) yield return null;

        if (waitForSlide != null && waitForSlide != this)
        {
            if (startWhenOtherArrives)
                while (!waitForSlide.HasArrived && !waitForSlide.IsFinished) yield return null;
            else
                while (!waitForSlide.IsFinished) yield return null;
        }

        float d = 0f;
        while (d < startDelay) { d += Dt; yield return null; }

        if (useAfterImages) CreateGhosts();
        PlaySE(whooshClip);
        trail.Clear();

        // ---- 1. 走行(だんだん加速)----
        float t = 0f;
        while (t < moveDuration)
        {
            t += Dt;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, moveDuration));
            float p = Mathf.Pow(k, accelPower);
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, p);

            float ramp = Mathf.Clamp01(k * 3f);
            ApplyPose(Mathf.Lerp(1f, moveStretchX, ramp), Mathf.Lerp(1f, moveSquashY, ramp), -moveLeanAngle * dir * ramp);
            RecordTrail();
            yield return null;
        }

        // ---- 2. 急ブレーキ(通り過ぎながら急減速・前のめり)----
        PlaySE(brakeClip);
        if (shakeTarget != null && shakeTarget != rt) StartCoroutine(ShakeRoutine());

        Vector2 overPos = endPos + moveDir * overshoot;
        t = 0f;
        while (t < brakeDuration)
        {
            t += Dt;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, brakeDuration));
            float e = 1f - Mathf.Pow(1f - k, 3f);
            rt.anchoredPosition = Vector2.LerpUnclamped(endPos, overPos, e);
            ApplyPose(
                Mathf.Lerp(moveStretchX, brakeSquashX, e),
                Mathf.Lerp(moveSquashY, brakeStretchY, e),
                Mathf.Lerp(-moveLeanAngle * dir, brakeLeanAngle * dir, e));
            RecordTrail();
            yield return null;
        }

        HasArrived = true;
        OnArrived?.Invoke();

        // ---- 3. 揺り戻し(減衰振動で指定位置に収まる)----
        t = 0f;
        while (t < settleDuration)
        {
            t += Dt;
            float s = Mathf.Clamp01(t / Mathf.Max(0.0001f, settleDuration));
            float decay = Mathf.Exp(-settleDamping * s) * Mathf.Cos(2f * Mathf.PI * settleFrequency * s);
            decay *= 1f - s; // 最後はぴったり0へ

            rt.anchoredPosition = endPos + moveDir * (overshoot * decay);
            ApplyPose(
                1f + (brakeSquashX - 1f) * decay,
                1f + (brakeStretchY - 1f) * decay,
                brakeLeanAngle * dir * decay);
            RecordTrail();
            UpdateGhostFade(s);
            yield return null;
        }

        rt.anchoredPosition = endPos;
        ApplyPose(1f, 1f, 0f);
        ClearGhosts();
        HasArrived = true;
        IsFinished = true;
    }

    private Vector2 CalcOffset()
    {
        float angle;
        switch (directionMode)
        {
            case DirectionMode.AlongTextAngle:
                angle = rt.localEulerAngles.z;
                break;
            case DirectionMode.CustomAngle:
                angle = moveAngle;
                break;
            case DirectionMode.Preset:
                return -PresetDir() * startDistance;
            default:
                return startOffset;
        }
        float rad = angle * Mathf.Deg2Rad;
        Vector2 axis = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)); // 線の右向き
        return axis * (comeFromRight ? startDistance : -startDistance);
    }

    // 進む向き(正規化)
    private Vector2 PresetDir()
    {
        float r = presetSlantAngle * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), sn = Mathf.Sin(r);
        switch (moveTo)
        {
            case MoveTo.Right: return Vector2.right;
            case MoveTo.Left: return Vector2.left;
            case MoveTo.Up: return Vector2.up;
            case MoveTo.Down: return Vector2.down;
            case MoveTo.UpRight: return new Vector2(c, sn);
            case MoveTo.UpLeft: return new Vector2(-c, sn);
            case MoveTo.DownRight: return new Vector2(c, -sn);
            default: return new Vector2(-c, -sn); // DownLeft
        }
    }

    // ---------- 姿勢(伸び縮み+傾き)----------
    // lean > 0 で上側が右へ倒れる
    private void ApplyPose(float sx, float sy, float lean)
    {
        rt.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);

        if (tmp != null)
        {
            ApplyShear(lean);
        }
        else
        {
            // TMP 以外(Image など)は回転で代用
            rt.localRotation = baseRotation * Quaternion.Euler(0f, 0f, -lean * 0.5f);
        }
    }

    private void ApplyShear(float angle)
    {
        // 斜め奥行きテキストが付いている場合は、そちらに傾きを渡して一緒に変形してもらう
        if (perspective != null && perspective.isActiveAndEnabled)
        {
            perspective.ExtraLeanDegrees = Mathf.Abs(angle) < 0.01f ? 0f : angle;
            perspective.Apply();
            return;
        }

        if (Mathf.Abs(angle) < 0.01f)
        {
            if (shearing) { tmp.ForceMeshUpdate(); shearing = false; }
            return;
        }

        tmp.ForceMeshUpdate();
        var info = tmp.textInfo;
        float tan = Mathf.Tan(angle * Mathf.Deg2Rad);

        float minY = float.MaxValue;
        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            var v = info.meshInfo[m].vertices;
            int n = info.meshInfo[m].vertexCount;
            for (int i = 0; i < n; i++) if (v[i].y < minY) minY = v[i].y;
        }
        if (minY == float.MaxValue) return;

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            var v = info.meshInfo[m].vertices;
            int n = info.meshInfo[m].vertexCount;
            for (int i = 0; i < n; i++) v[i].x += (v[i].y - minY) * tan;
        }
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        shearing = true;
    }

    // ---------- 画面揺れ ----------
    private IEnumerator ShakeRoutine()
    {
        Vector2 basePos = shakeTarget.anchoredPosition;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Dt;
            float power = 1f - Mathf.Clamp01(t / shakeDuration);
            shakeTarget.anchoredPosition = basePos + UnityEngine.Random.insideUnitCircle * shakeAmount * power;
            yield return null;
        }
        shakeTarget.anchoredPosition = basePos;
    }

    // ---------- 残像 ----------
    private void CreateGhosts()
    {
        ClearGhosts();
        int sibling = rt.GetSiblingIndex();

        for (int i = 0; i < afterImageCount; i++)
        {
            var go = new GameObject($"{name}_AfterImage{i}", typeof(RectTransform));
            var grt = (RectTransform)go.transform;
            grt.SetParent(rt.parent, false);
            grt.SetSiblingIndex(sibling); // 本体の後ろに並べる
            grt.anchorMin = rt.anchorMin;
            grt.anchorMax = rt.anchorMax;
            grt.pivot = rt.pivot;
            grt.sizeDelta = rt.sizeDelta;
            grt.localRotation = rt.localRotation;
            grt.anchoredPosition = rt.anchoredPosition;

            Graphic g = null;
            if (tmp != null)
            {
                var t = go.AddComponent<TextMeshProUGUI>();
                t.font = tmp.font;
                t.fontSharedMaterial = tmp.fontSharedMaterial;
                t.text = tmp.text;
                t.fontSize = tmp.fontSize;
                t.enableAutoSizing = tmp.enableAutoSizing;
                t.fontSizeMin = tmp.fontSizeMin;
                t.fontSizeMax = tmp.fontSizeMax;
                t.fontStyle = tmp.fontStyle;
                t.alignment = tmp.alignment;
                t.characterSpacing = tmp.characterSpacing;
                t.wordSpacing = tmp.wordSpacing;
                t.lineSpacing = tmp.lineSpacing;
                t.textWrappingMode = tmp.textWrappingMode;
                t.overflowMode = tmp.overflowMode;
                t.margin = tmp.margin;

                // 本体に斜め奥行きが付いていれば、残像にも同じ形を付ける
                if (perspective != null && perspective.isActiveAndEnabled)
                {
                    var gp = go.AddComponent<TMPPerspectiveTextEffect>();
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(perspective), gp);
                    gp.SetTarget(t);
                }
                g = t;
            }
            else if (graphic is Image srcImg)
            {
                var img = go.AddComponent<Image>();
                img.sprite = srcImg.sprite;
                img.type = srcImg.type;
                img.preserveAspect = srcImg.preserveAspect;
                g = img;
            }

            if (g == null) { Destroy(go); continue; }
            g.raycastTarget = false;
            g.color = GhostColor(i, 1f);
            ghosts.Add(g);
        }
    }

    private Color GhostColor(int i, float fade)
    {
        Color c = graphic != null ? graphic.color : Color.white;
        float a = afterImageAlpha * (1f - (float)i / Mathf.Max(1, afterImageCount)) * fade;
        c.a *= a;
        return c;
    }

    private void RecordTrail()
    {
        trail.Add(rt.anchoredPosition);

        for (int i = 0; i < ghosts.Count; i++)
        {
            if (ghosts[i] == null) continue;
            int idx = Mathf.Max(0, trail.Count - 1 - (i + 1) * afterImageFrameGap);
            var grt = (RectTransform)ghosts[i].transform;
            grt.anchoredPosition = trail[idx];
            grt.localScale = rt.localScale;
        }
    }

    private void UpdateGhostFade(float settleProgress)
    {
        if (ghosts.Count == 0) return;
        float settleTime = settleProgress * settleDuration;
        float fade = 1f - Mathf.Clamp01(settleTime / Mathf.Max(0.0001f, afterImageFadeTime));
        for (int i = 0; i < ghosts.Count; i++)
            if (ghosts[i] != null) ghosts[i].color = GhostColor(i, fade);
        if (fade <= 0f) ClearGhosts();
    }

    private void ClearGhosts()
    {
        foreach (var g in ghosts) if (g != null) Destroy(g.gameObject);
        ghosts.Clear();
    }

    private void PlaySE(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private void OnDisable()
    {
        ClearGhosts();
    }
}