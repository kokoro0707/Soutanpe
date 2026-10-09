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

    [Header("ラウンド連動(バトルシーン用)")]
    [Tooltip("ONにすると、次のラウンドに進んだ時にBGMを頭から流し直す")]
    [SerializeField] private bool restartOnNewRound = false;
    [Tooltip("未設定ならシーン内から自動で探す")]
    [SerializeField] private MatchScoreManager matchScoreManager;
    [Tooltip("ラウンド切り替え時のフェード時間(秒)。0で即頭出し")]
    [SerializeField] private float roundRestartFade = 0.3f;
    [Tooltip("Random Bgms を使っている時、ラウンドごとに曲を選び直す")]
    [SerializeField] private bool pickNewRandomEachRound = false;

    [Header("試合終了時(ラウンド連動ONの時のみ)")]
    [Tooltip("試合が決着したらBGMを止める")]
    [SerializeField] private bool stopOnMatchEnd = false;
    [Tooltip("試合が決着したら流す曲(勝利ジングル等)。未設定なら何もしない")]
    [SerializeField] private AudioClip matchEndBgm;
    [SerializeField] private bool matchEndBgmLoop = false;
    [SerializeField] private float matchEndFade = 1f;

    [Header("シーン終了時")]
    [Tooltip("このシーンから出る時に曲を止める(次のシーンのSceneBGMに任せるなら OFF のままでOK)")]
    [SerializeField] private bool stopOnDestroy = false;

    private AudioClip currentClip;
    private int lastRound = -1;
    private bool matchEndHandled;

    private void Start()
    {
        if (restartOnNewRound && matchScoreManager == null)
            matchScoreManager = FindFirstObjectByType<MatchScoreManager>();
        if (matchScoreManager != null)
            lastRound = matchScoreManager.CurrentRound;

        StartCoroutine(PlayRoutine());
    }

    private void Update()
    {
        if (!restartOnNewRound || matchScoreManager == null) return;

        // 試合決着
        if (matchScoreManager.MatchIsOver)
        {
            if (!matchEndHandled)
            {
                matchEndHandled = true;
                HandleMatchEnd();
            }
            return;
        }
        matchEndHandled = false;

        // ラウンドが進んだ(または再戦でラウンド1に戻った)
        int round = matchScoreManager.CurrentRound;
        if (round != lastRound)
        {
            lastRound = round;
            RestartForNewRound();
        }
    }

    /// <summary>BGMを頭から流し直す(外部から呼んでもOK)</summary>
    public void RestartForNewRound()
    {
        AudioManager am = GetOrCreateAudioManager();
        if (am == null) return;

        if (pickNewRandomEachRound || currentClip == null)
            currentClip = PickClip();
        if (currentClip == null) return;

        am.PlayBGM(currentClip, volume, roundRestartFade, loop, true);
        Debug.Log($"[SceneBGM] ラウンド{lastRound} 開始 → BGMを頭から再生", this);
    }

    private void HandleMatchEnd()
    {
        AudioManager am = GetOrCreateAudioManager();
        if (am == null) return;

        if (matchEndBgm != null)
            am.PlayBGM(matchEndBgm, volume, matchEndFade, matchEndBgmLoop, true);
        else if (stopOnMatchEnd)
            am.StopBGM(matchEndFade);
    }

    private IEnumerator PlayRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        AudioManager am = GetOrCreateAudioManager();
        if (am == null) yield break;

        AudioClip clip = PickClip();
        currentClip = clip;
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