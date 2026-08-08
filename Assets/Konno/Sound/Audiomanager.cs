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
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("再生用ソース(未設定なら自動生成)")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;

    private const string BgmVolumeKey = "BGMVolume";
    private const string SfxVolumeKey = "SEVolume";
    private const string VoiceVolumeKey = "VoiceVolume";
    private const string BgmMutedKey = "BGMMuted";
    private const string SfxMutedKey = "SEMuted";
    private const string VoiceMutedKey = "VoiceMuted";

    /// <summary>現在のBGM音量(0から1、ミュート状態は含まない設定値そのもの)</summary>
    public float BgmVolume { get; private set; } = 1f;
    /// <summary>現在のSE音量(0から1、ミュート状態は含まない設定値そのもの)</summary>
    public float SfxVolume { get; private set; } = 1f;
    /// <summary>現在のVoice音量(0から1、ミュート状態は含まない設定値そのもの)</summary>
    public float VoiceVolume { get; private set; } = 1f;

    public bool BgmMuted { get; private set; }
    public bool SfxMuted { get; private set; }
    public bool VoiceMuted { get; private set; }

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

        if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;

        LoadSavedSettings();
    }

    private void LoadSavedSettings()
    {
        BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        VoiceVolume = PlayerPrefs.GetFloat(VoiceVolumeKey, 1f);

        BgmMuted = PlayerPrefs.GetInt(BgmMutedKey, 0) == 1;
        SfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
        VoiceMuted = PlayerPrefs.GetInt(VoiceMutedKey, 0) == 1;

        ApplyBgmVolume();
        // SE/VoiceはPlayOneShotのたびに音量を渡す方式なので、ここでは値の保持のみ
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

    /// <summary>Voice音量を設定して保存する(0から1)</summary>
    public void SetVoiceVolume(float value)
    {
        VoiceVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VoiceVolumeKey, VoiceVolume);
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

    public void SetVoiceMuted(bool muted)
    {
        VoiceMuted = muted;
        PlayerPrefs.SetInt(VoiceMutedKey, muted ? 1 : 0);
    }

    private void ApplyBgmVolume()
    {
        if (bgmSource != null) bgmSource.volume = BgmMuted ? 0f : BgmVolume;
    }

    /// <summary>
    /// BGMを再生する。同じクリップが再生中なら何もしない(頭出しされない)。
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = BgmMuted ? 0f : BgmVolume;
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
        sfxSource.PlayOneShot(clip, SfxVolume);
    }

    /// <summary>
    /// ボイスを1回再生する。
    /// </summary>
    public void PlayVoice(AudioClip clip)
    {
        if (clip == null || voiceSource == null || VoiceMuted) return;
        voiceSource.PlayOneShot(clip, VoiceVolume);
    }
}