using UnityEngine;

/// <summary>
/// BGM/SEの再生と音量管理をまとめるシングルトン。
/// シーンをまたいで存在し続ける(DontDestroyOnLoad)。
///
/// 使い方(BGM再生の例):
///   AudioManager.Instance.PlayBGM(titleBgmClip);
///
/// 使い方(SE再生の例、攻撃ヒット時など):
///   AudioManager.Instance.PlaySFX(hitSeClip);
///
/// マスター音量(MasterVolume)は、BGM/SEそれぞれの音量に掛け算される形で反映される。
/// BGM/SE個別の音量設定・保存ロジック自体は変更していない。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("再生用ソース(未設定なら自動生成)")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("初期音量(まだ一度も設定を保存していない状態でのデフォルト値, 0から1)")]
    [Tooltip("10段階ゲージでの初期表示に合わせる場合は 0.5 = レベル5")]
    [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float defaultBgmVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.5f;

    private const string MasterVolumeKey = "MasterVolume";
    private const string BgmVolumeKey = "BGMVolume";
    private const string SfxVolumeKey = "SEVolume";
    private const string MasterMutedKey = "MasterMuted";
    private const string BgmMutedKey = "BGMMuted";
    private const string SfxMutedKey = "SEMuted";

    /// <summary>現在のマスター音量(0から1、ミュート状態は含まない設定値そのもの)。BGM/SEに掛け算される</summary>
    public float MasterVolume { get; private set; } = 1f;
    /// <summary>現在のBGM音量(0から1、ミュート状態は含まない設定値そのもの)</summary>
    public float BgmVolume { get; private set; } = 1f;
    /// <summary>現在のSE音量(0から1、ミュート状態は含まない設定値そのもの)</summary>
    public float SfxVolume { get; private set; } = 1f;

    public bool MasterMuted { get; private set; }
    public bool BgmMuted { get; private set; }
    public bool SfxMuted { get; private set; }

    private void Awake()
    {
        // シーンをまたいでも1つだけ存在するようにする
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        LoadSavedSettings();
    }

    private void LoadSavedSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, defaultBgmVolume);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);

        MasterMuted = PlayerPrefs.GetInt(MasterMutedKey, 0) == 1;
        BgmMuted = PlayerPrefs.GetInt(BgmMutedKey, 0) == 1;
        SfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;

        ApplyBgmVolume();
        // SEはPlayOneShotのたびに音量を渡す方式なので、ここでは値の保持のみ
    }

    /// <summary>マスター音量を設定して保存する(0から1)。BGM/SE両方に反映される</summary>
    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        ApplyBgmVolume();
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
    }

    /// <summary>BGM音量を設定して保存する(0から1)</summary>
    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
        ApplyBgmVolume();
        PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
    }

    /// <summary>SE音量を設定して保存する(0から1)</summary>
    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
    }

    public void SetMasterMuted(bool muted)
    {
        MasterMuted = muted;
        ApplyBgmVolume();
        PlayerPrefs.SetInt(MasterMutedKey, muted ? 1 : 0);
    }

    public void SetBgmMuted(bool muted)
    {
        BgmMuted = muted;
        ApplyBgmVolume();
        PlayerPrefs.SetInt(BgmMutedKey, muted ? 1 : 0);
    }

    public void SetSfxMuted(bool muted)
    {
        SfxMuted = muted;
        PlayerPrefs.SetInt(SfxMutedKey, muted ? 1 : 0);
    }

    /// <summary>マスターがミュートなら、他のミュート状態に関わらず無音になる</summary>
    private float GetEffectiveMasterVolume()
    {
        return MasterMuted ? 0f : MasterVolume;
    }

    private void ApplyBgmVolume()
    {
        if (bgmSource == null) return;
        bgmSource.volume = BgmMuted ? 0f : BgmVolume * GetEffectiveMasterVolume();
    }

    /// <summary>
    /// BGMを再生する。同じクリップが再生中なら何もしない(頭出しされない)。
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = BgmMuted ? 0f : BgmVolume * GetEffectiveMasterVolume();
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    /// <summary>
    /// 効果音を1回再生する(PlayOneShotなので重なって鳴らしてもOK)。
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null || SfxMuted) return;
        sfxSource.PlayOneShot(clip, SfxVolume * GetEffectiveMasterVolume());
    }
}