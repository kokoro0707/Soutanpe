using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// キャラクターの攻撃・必殺技のSEを鳴らすスクリプト。
/// キャラ本体のスクリプトは書き換えずに使える。
///
/// 鳴らし方は2通り(併用OK):
///  A) 自動:Animator のステート名を登録しておくと、そのステートに入った瞬間に鳴る
///     (例: State Name に "Attack1" → 攻撃アニメが始まったら鳴る)
///  B) 手動:アニメーションイベントやスクリプトから呼ぶ
///     PlayAttack(番号) / PlaySpecial(番号) / PlayByLabel("名前") / PlayClip(AudioClip)
///
/// 音量は「設定画面のマスター × SE音量 × 各SEのVolume」になる(AudioManagerと連動)。
/// </summary>
public class FighterSE : MonoBehaviour
{
    [Serializable]
    public class SeEntry
    {
        [Tooltip("分かりやすい名前(PlayByLabelで使う)")]
        public string label = "Attack";

        [Tooltip("自動再生用:このAnimatorステートに入ったら鳴らす(空なら自動では鳴らない)")]
        public string animatorStateName;

        [Tooltip("鳴らす音。複数入れるとランダムで1つ選ぶ")]
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("ピッチ(音の高さ)のランダム幅。両方1で変化なし")]
        [Range(0.5f, 2f)] public float pitchMin = 0.95f;
        [Range(0.5f, 2f)] public float pitchMax = 1.05f;

        [Tooltip("ステートに入ってから鳴るまでの遅れ(秒)。振りかぶり後に鳴らしたい時など")]
        public float delay = 0f;

        [NonSerialized] public int stateHash;
    }

    [Serializable]
    public class CharacterSESet
    {
        [Tooltip("分かりやすい名前(キャラ名など)")]
        public string name = "Character";

        [Tooltip("このキャラの Animator Controller(Override Controller も可)。" +
                 "キャラが変わって Animator の Controller が切り替わると、自動でこのセットに切り替わる")]
        public RuntimeAnimatorController controller;

        public List<SeEntry> attackSEs = new List<SeEntry>();
        public List<SeEntry> specialSEs = new List<SeEntry>();
    }

    [Header("キャラ別SE(キャラが入れ替わる場合)")]
    [Tooltip("Animator の Controller が一致したセットを使う。どれにも一致しない時は下の『攻撃SE/必殺技SE』を使う")]
    [SerializeField] private List<CharacterSESet> characterSets = new List<CharacterSESet>();

    [Header("攻撃SE(共通・キャラ別セットに一致しない時)")]
    [SerializeField] private List<SeEntry> attackSEs = new List<SeEntry>();

    [Header("必殺技SE(共通・キャラ別セットに一致しない時)")]
    [SerializeField] private List<SeEntry> specialSEs = new List<SeEntry>();

    [Header("自動再生(Animator連動)")]
    [Tooltip("未設定なら自分か子オブジェクトから自動で探す")]
    [SerializeField] private Animator animator;
    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private bool autoPlayByAnimatorState = true;

    [Header("その他")]
    [Tooltip("同じSEが短時間に連続で鳴るのを防ぐ間隔(秒)")]
    [SerializeField] private float minInterval = 0.05f;
    [Tooltip("同時に鳴らせる数(足りないと古い音が途切れる)")]
    [SerializeField, Range(1, 8)] private int voices = 4;

    private AudioSource[] sources;
    private int nextSource;
    private int lastStateHash;
    private float lastNormalizedTime;
    private readonly Dictionary<SeEntry, float> lastPlayTime = new Dictionary<SeEntry, float>();

    // 今使っているSEリスト(キャラ別セット or 共通)
    private List<SeEntry> activeAttack;
    private List<SeEntry> activeSpecial;
    private RuntimeAnimatorController lastController;
    private bool setSelected;

    /// <summary>今使っているキャラ別セットの名前(共通の場合は "Default")</summary>
    public string ActiveSetName { get; private set; } = "Default";

    private void Awake()
    {
        if (animator == null) animator = FindAnimator();

        sources = new AudioSource[voices];
        for (int i = 0; i < voices; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2Dサウンド
            sources[i] = src;
        }

        // 全セットのステート名をハッシュ化しておく
        CacheHashes(attackSEs);
        CacheHashes(specialSEs);
        foreach (var set in characterSets)
        {
            CacheHashes(set.attackSEs);
            CacheHashes(set.specialSEs);
        }

        SelectSet();
    }

    private static void CacheHashes(List<SeEntry> list)
    {
        if (list == null) return;
        foreach (var e in list)
            e.stateHash = string.IsNullOrEmpty(e.animatorStateName) ? 0 : Animator.StringToHash(e.animatorStateName);
    }

    /// <summary>
    /// 今の Animator Controller に合うSEセットを選び直す。
    /// キャラ入れ替え時は自動で呼ばれるが、手動で呼んでもOK。
    /// </summary>
    public void SelectSet()
    {
        if (animator == null) animator = FindAnimator();
        RuntimeAnimatorController ctrl = animator != null ? animator.runtimeAnimatorController : null;
        lastController = ctrl;
        setSelected = true;

        activeAttack = attackSEs;
        activeSpecial = specialSEs;
        ActiveSetName = "Default";

        if (ctrl != null)
        {
            CharacterSESet found = FindSet(ctrl);
            if (found != null)
            {
                activeAttack = found.attackSEs;
                activeSpecial = found.specialSEs;
                ActiveSetName = found.name;
            }
        }

        // 切り替わった直後に、今のステートで誤って鳴らないようにする
        lastStateHash = 0;
        lastNormalizedTime = 0f;
        Debug.Log($"[FighterSE] {name}: SEセット = {ActiveSetName} " +
                  $"(Animator = {(animator != null ? animator.name : "なし")}, Controller = {(ctrl != null ? ctrl.name : "なし")})", this);
    }

    // 子にAnimatorが複数ある場合は、Controllerが入っていて有効なものを優先する
    private Animator FindAnimator()
    {
        Animator fallback = null;
        foreach (var a in GetComponentsInChildren<Animator>(true))
        {
            if (a.runtimeAnimatorController == null) continue;
            if (a.isActiveAndEnabled) return a;
            if (fallback == null) fallback = a;
        }
        return fallback;
    }

    // Controller に合うセットを探す(4段階で判定)
    private CharacterSESet FindSet(RuntimeAnimatorController ctrl)
    {
        // 1) 完全一致
        foreach (var set in characterSets)
            if (set.controller != null && set.controller == ctrl) return set;

        // 2) Override Controller の場合は、元になっている Controller で一致
        var ov = ctrl as AnimatorOverrideController;
        if (ov != null && ov.runtimeAnimatorController != null)
            foreach (var set in characterSets)
                if (set.controller != null && set.controller == ov.runtimeAnimatorController) return set;

        // 3) 名前で一致(スクリプトで実行中に複製された Controller など。末尾の "(Clone)" は無視)
        string ctrlName = ctrl.name.Replace("(Clone)", "").Trim();
        foreach (var set in characterSets)
            if (set.controller != null && set.controller.name == ctrlName) return set;

        // 4) 登録したステート名が、今の Animator に実際にあるセット
        if (animator != null && animatorLayer < animator.layerCount)
        {
            foreach (var set in characterSets)
            {
                foreach (var e in Concat(set.attackSEs, set.specialSEs))
                {
                    if (e.stateHash != 0 && animator.HasState(animatorLayer, e.stateHash))
                        return set;
                }
            }
        }
        return null;
    }

    private static IEnumerable<SeEntry> Concat(List<SeEntry> a, List<SeEntry> b)
    {
        if (a != null) foreach (var e in a) yield return e;
        if (b != null) foreach (var e in b) yield return e;
    }

    private IEnumerable<SeEntry> AllEntries()
    {
        if (activeAttack != null) foreach (var e in activeAttack) yield return e;
        if (activeSpecial != null) foreach (var e in activeSpecial) yield return e;
    }

    // ---------- 自動再生 ----------
    private void Update()
    {
        if (animator == null) animator = FindAnimator();

        // キャラが入れ替わった(Animator Controller が変わった・Animatorが差し替わった)
        if (animator != null && (!setSelected || animator.runtimeAnimatorController != lastController))
            SelectSet();

        if (!autoPlayByAnimatorState || animator == null || !animator.isActiveAndEnabled) return;
        if (animatorLayer >= animator.layerCount) return;

        var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
        int hash = info.shortNameHash;
        float nt = info.normalizedTime;

        // 別のステートに入った / 同じステートが頭から再生し直された(連打など)
        bool entered = hash != lastStateHash || nt < lastNormalizedTime - 0.5f;
        lastStateHash = hash;
        lastNormalizedTime = nt;
        if (!entered) return;

        foreach (var e in AllEntries())
            if (e.stateHash != 0 && e.stateHash == hash)
                Play(e);
    }

    // ---------- 手動(アニメーションイベント・スクリプトから) ----------

    /// <summary>攻撃SEを番号で鳴らす(0から)</summary>
    public void PlayAttack(int index)
    {
        if (activeAttack != null && index >= 0 && index < activeAttack.Count) Play(activeAttack[index]);
    }

    /// <summary>必殺技SEを番号で鳴らす(0から)</summary>
    public void PlaySpecial(int index)
    {
        if (activeSpecial != null && index >= 0 && index < activeSpecial.Count) Play(activeSpecial[index]);
    }

    /// <summary>Labelの名前で鳴らす(攻撃・必殺技どちらからでも探す)</summary>
    public void PlayByLabel(string label)
    {
        foreach (var e in AllEntries())
            if (e.label == label) { Play(e); return; }
        Debug.LogWarning($"[FighterSE] Label '{label}' が見つかりません", this);
    }

    /// <summary>AudioClipを直接鳴らす(アニメーションイベントのObject欄にClipを入れて使える)</summary>
    public void PlayClip(AudioClip clip)
    {
        if (clip != null) PlayOne(clip, 1f, 1f);
    }

    // ---------- 再生処理 ----------
    private void Play(SeEntry e)
    {
        if (e == null || e.clips == null || e.clips.Length == 0) return;

        float now = Time.unscaledTime;
        if (lastPlayTime.TryGetValue(e, out float last) && now - last < minInterval) return;
        lastPlayTime[e] = now;

        AudioClip clip = e.clips[UnityEngine.Random.Range(0, e.clips.Length)];
        if (clip == null) return;

        float pitch = UnityEngine.Random.Range(Mathf.Min(e.pitchMin, e.pitchMax), Mathf.Max(e.pitchMin, e.pitchMax));

        if (e.delay > 0f) StartCoroutine(PlayDelayed(clip, e.volume, pitch, e.delay));
        else PlayOne(clip, e.volume, pitch);
    }

    private IEnumerator PlayDelayed(AudioClip clip, float volume, float pitch, float delay)
    {
        yield return new WaitForSeconds(delay); // ヒットストップ等で止まっている間は待つ
        PlayOne(clip, volume, pitch);
    }

    private void PlayOne(AudioClip clip, float volume, float pitch)
    {
        float vol = volume * GetSeVolume();
        if (vol <= 0f) return;

        var src = sources[nextSource];
        nextSource = (nextSource + 1) % sources.Length;

        src.pitch = pitch;
        src.clip = clip;
        src.volume = vol;
        src.Play();
    }

    // AudioManager の設定(マスター・SE音量・ミュート)に合わせる
    private static float GetSeVolume()
    {
        var am = AudioManager.Instance;
        if (am == null) return 1f;
        if (am.MasterMuted || am.SfxMuted) return 0f;
        return am.MasterVolume * am.SfxVolume;
    }
}