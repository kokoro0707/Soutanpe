using UnityEngine;

/// <summary>
/// バトル開始時に RoundAnnouncementController の「ROUND1」演出を再生し、
/// 演出中はゲームを一時停止する(これまでの ReadyFightManager と同じ役割を引き継ぐ)。
///
/// 使い方:
///   1. このスクリプトをバトルシーンの空のGameObject(例: BattleStart)にアタッチ
///   2. Inspectorで Round Announcement に RoundAnnouncementController をドラッグ&ドロップ
///   3. RoundAnnouncementController 側の Inspector で
///      On Fight Start イベントに、このコンポーネントの ResumeGame() を登録する
///      (これで「ROUND1」が消えたタイミングで Time.timeScale が 1 に戻り、操作が解禁される)
///   4. 旧 ReadyFightManager がシーンに残っている場合は、
///      そのコンポーネントのチェックボックスをオフにして無効化しておく
///      (Start() コーエンチンが丸ごと動かなくなるため、READY/FIGHT表示は出なくなる)
/// </summary>
public class BattleStartController : MonoBehaviour
{
    [Tooltip("再生するROUND演出。Play(1)が呼ばれる")]
    [SerializeField] private RoundAnnouncementController roundAnnouncement;

    [Tooltip("演出中、ゲームを一時停止する(ReadyFightManagerと同じ挙動にしたい場合はON)")]
    [SerializeField] private bool pauseDuringAnnouncement = true;

    [Tooltip("何ラウンド目として表示するか(ROUND{この数字})。" +
             "シーン開始時(Start())にはこの値が使われる")]
    [SerializeField] private int roundNumber = 1;

    private void Start()
    {
        PlayRound(roundNumber);
    }

    /// <summary>
    /// 指定したラウンド数で「ROUND{number}」演出を再生する。
    /// シーン開始時のRound1だけでなく、MatchScoreManagerが2ラウンド目・3ラウンド目を
    /// 開始する際にも、このメソッドを呼び出して同じ演出を再利用する。
    /// </summary>
    public void PlayRound(int number)
    {
        roundNumber = number;

        if (pauseDuringAnnouncement)
        {
            Time.timeScale = 0f;
        }

        if (roundAnnouncement != null)
        {
            roundAnnouncement.Play(roundNumber);
        }
        else
        {
            Debug.LogWarning("[BattleStartController] Round Announcement が未設定です。", this);

            // 演出が無い場合でも、timeScaleを止めたままにしないよう即座に戻す
            if (pauseDuringAnnouncement)
            {
                Time.timeScale = 1f;
            }
        }
    }

    /// <summary>
    /// RoundAnnouncementController の OnFightStart イベントに登録する。
    /// 「ROUND1」が画面から消えたタイミングで呼ばれ、ゲームの時間を再開する。
    /// </summary>
    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }
}