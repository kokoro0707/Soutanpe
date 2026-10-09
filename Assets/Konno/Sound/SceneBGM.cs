using System.Collections;
using UnityEngine;

/// <summary>
/// シーンごとのBGM設定。各シーンに1つ置いておくと、シーン開始時にそのBGMが流れる。
/// (メニュー画面・キャラクター選択・バトルなど、それぞれのシーンに置いて曲を指定する)
///
/// ・前のシーンと同じ曲なら、途切れずにそのまま流れ続ける
/// ・違う曲なら、フェードで切り替わる
/// ・音量は設定画面のBGM音量 × Volume で決まる
///
/// 注意:AudioManager と同じGameObjectには付けないこと
/// (AudioManagerの2つ目は GameObject ごと消されるため、一緒に消えてしまう)
/// </summary>
public class SceneBGM : MonoBehaviour
{
    [Header("BGM")]
    [Tooltip("このシーンで流す曲")]
    [SerializeField] private AudioClip bgm;

    [Tooltip("ここに複数入れると、その中からランダムで1曲流す(バトル用など)。空なら上の BGM を使う")]
    [SerializeField] private AudioClip[] randomBgms;

    [Tooltip("曲ごとの音量(設定画面のBGM音量に掛け算される)")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [SerializeField] private bool loop = true;

    [Header("切り替え")]
    [Tooltip("前の曲からの切り替えにかける時間(秒)。0で即切り替え")]
    [SerializeField] private float fadeTime = 0.5f;

    [Tooltip("シーン開始から曲が流れ始めるまでの待ち時間(秒)")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("前のシーンと同じ曲でも、頭から流し直す")]
    [SerializeField] private bool restartIfSame = false;

    [Tooltip("BGMが未設定の時、前のシーンの曲を止める(OFFならそのまま流し続ける)")]
    [SerializeField] private bool stopIfEmpty = true;

    [Header("シーン終了時")]
    [Tooltip("このシーンから出る時に曲を止める(次のシーンのSceneBGMに任せるなら OFF のままでOK)")]
    [SerializeField] private bool stopOnDestroy = false;

    private void Start()
    {
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        AudioManager am = GetOrCreateAudioManager();
        if (am == null) yield break;

        AudioClip clip = PickClip();
        if (clip != null)
        {
            am.PlayBGM(clip, volume, fadeTime, loop, restartIfSame);
        }
        else if (stopIfEmpty)
        {
            am.StopBGM(fadeTime);
        }
    }

    private AudioClip PickClip()
    {
        if (randomBgms != null && randomBgms.Length > 0)
        {
            // 未設定の要素は除いてランダムに選ぶ
            int valid = 0;
            foreach (var c in randomBgms) if (c != null) valid++;
            if (valid > 0)
            {
                int pick = Random.Range(0, valid);
                foreach (var c in randomBgms)
                {
                    if (c == null) continue;
                    if (pick-- == 0) return c;
                }
            }
        }
        return bgm;
    }

    // このシーンを直接再生した時など、AudioManagerが居なければ自動で作る
    private static AudioManager GetOrCreateAudioManager()
    {
        if (AudioManager.Instance != null) return AudioManager.Instance;

        var existing = FindFirstObjectByType<AudioManager>();
        if (existing != null) return existing;

        Debug.Log("[SceneBGM] AudioManager が無いので自動で作成しました");
        var go = new GameObject("AudioManager");
        return go.AddComponent<AudioManager>();
    }

    private void OnDestroy()
    {
        if (stopOnDestroy && AudioManager.Instance != null)
            AudioManager.Instance.StopBGM(fadeTime);
    }
}
