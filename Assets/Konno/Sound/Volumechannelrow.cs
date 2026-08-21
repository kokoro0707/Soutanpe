using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 設定画面の1行分(Master / BGM / SE のいずれか)。
/// ミュートアイコン、10段階ゲージ、数値テキストをまとめて管理し、
/// 実際の音量はAudioManagerに反映する。
/// </summary>
public class VolumeChannelRow : MonoBehaviour
{
    public enum ChannelType { Master, Bgm, Sfx }

    [Header("チャンネル種別")]
    [SerializeField] private ChannelType channelType;

    [Header("参照")]
    [SerializeField] private SegmentedVolumeBar volumeBar;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image muteIconImage;

    [Header("ミュート時のアイコン切り替え(任意)")]
    [SerializeField] private Sprite iconOnSprite;
    [SerializeField] private Sprite iconOffSprite;

    private int level = 10; // 0から10の整数レベルでUI表示(内部のAudioManagerには /10f して渡す)
    private bool isMuted;

    private void OnEnable()
    {
        SyncFromAudioManager();
    }

    /// <summary>
    /// AudioManagerに保存されている現在値を読み込んでUIに反映する。
    /// </summary>
    public void SyncFromAudioManager()
    {
        if (AudioManager.Instance == null) return;

        float volume;
        switch (channelType)
        {
            case ChannelType.Master:
                volume = AudioManager.Instance.MasterVolume;
                isMuted = AudioManager.Instance.MasterMuted;
                break;
            case ChannelType.Bgm:
                volume = AudioManager.Instance.BgmVolume;
                isMuted = AudioManager.Instance.BgmMuted;
                break;
            case ChannelType.Sfx:
                volume = AudioManager.Instance.SfxVolume;
                isMuted = AudioManager.Instance.SfxMuted;
                break;
            default:
                volume = 0f;
                isMuted = false;
                break;
        }

        level = Mathf.RoundToInt(volume * (volumeBar != null ? volumeBar.MaxLevel : 10));
        RefreshVisual();
    }

    /// <summary>ゲージのレベルを1段階上げる(十字キー右)</summary>
    public void Increase()
    {
        SetLevel(level + 1);
    }

    /// <summary>ゲージのレベルを1段階下げる(十字キー左)</summary>
    public void Decrease()
    {
        SetLevel(level - 1);
    }

    /// <summary>ミュートON/OFFを切り替える(Aボタン)</summary>
    public void ToggleMute()
    {
        isMuted = !isMuted;
        ApplyMuted();
        RefreshVisual();
    }

    private void SetLevel(int newLevel)
    {
        int max = volumeBar != null ? volumeBar.MaxLevel : 10;
        level = Mathf.Clamp(newLevel, 0, max);

        ApplyVolume();
        RefreshVisual();
    }

    private void ApplyVolume()
    {
        if (AudioManager.Instance == null) return;

        int max = volumeBar != null ? volumeBar.MaxLevel : 10;
        float normalized = max > 0 ? (float)level / max : 0f;

        switch (channelType)
        {
            case ChannelType.Master:
                AudioManager.Instance.SetMasterVolume(normalized);
                break;
            case ChannelType.Bgm:
                AudioManager.Instance.SetBgmVolume(normalized);
                break;
            case ChannelType.Sfx:
                AudioManager.Instance.SetSfxVolume(normalized);
                break;
        }
    }

    private void ApplyMuted()
    {
        if (AudioManager.Instance == null) return;

        switch (channelType)
        {
            case ChannelType.Master:
                AudioManager.Instance.SetMasterMuted(isMuted);
                break;
            case ChannelType.Bgm:
                AudioManager.Instance.SetBgmMuted(isMuted);
                break;
            case ChannelType.Sfx:
                AudioManager.Instance.SetSfxMuted(isMuted);
                break;
        }
    }

    private void RefreshVisual()
    {
        if (volumeBar != null) volumeBar.SetLevel(level);
        if (levelText != null) levelText.text = level.ToString();

        if (muteIconImage != null)
        {
            if (isMuted && iconOffSprite != null) muteIconImage.sprite = iconOffSprite;
            else if (!isMuted && iconOnSprite != null) muteIconImage.sprite = iconOnSprite;
        }
    }

    /// <summary>
    /// このRowが現在キー操作の対象として選択されているかどうかの見た目を切り替える。
    /// SettingsNavigatorから呼ばれる。
    /// </summary>
    public void SetFocused(bool focused)
    {
        if (volumeBar != null) volumeBar.SetFocused(focused);
    }
}