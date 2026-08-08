using UnityEngine;

/// <summary>
/// タイトル画面が始まったらBGMを自動再生する。
/// タイトルシーンのCanvasなど、常に存在するGameObjectにアタッチする。
///
/// AudioManagerはDontDestroyOnLoadで消えないため、
/// 他のシーンに遷移してもこのBGMは鳴り続ける
/// (シーンごとに音楽を切り替えたい場合は、次のシーン側にも
///  同様のスクリプトを置いて別のクリップでPlayBGMを呼べば自動的に切り替わる)。
/// </summary>
public class TitleBgmPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip titleBgmClip;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[TitleBgmPlayer] AudioManagerが見つかりません。シーンにAudioManagerを配置してください。", this);
            return;
        }

        AudioManager.Instance.PlayBGM(titleBgmClip);
    }
}