using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// スト6のような「2ラウンド先取で勝利、1-1なら最終ラウンド(3R目)で決着」という
/// スコア制で試合を進行させる管理役。
///
/// KoSequenceController側で1ラウンドのKOが決まったタイミングで RegisterRoundResult() を
/// 呼んでもらい、このスクリプトが
///   ・P1/P2のラウンド取得数を加算(引き分けの場合はどちらも加算しない)
///   ・画面中央上部のスコアテキスト(例: "P1 1 - 0 P2")を更新
///   ・まだ2ラウンド先取していなければ、次ラウンドの「ROUND2」「ROUND3」演出を再生
///   ・2ラウンド先取した時点で試合終了と判定し、OnMatchOverイベントを発火
/// を行う。
///
/// 使い方:
///   1. シーン内の常にアクティブなオブジェクト(KoSequenceControllerと同じ場所でよい)に
///      このスクリプトをアタッチする
///   2. Score Textに、画面中央上部に配置したTMP_Text(スコア表示用)を割り当てる
///   3. Battle Start Controllerに、ROUND演出の再生を行っているBattleStartControllerを
///      割り当てる(次ラウンドの演出を再生するのに使う)
///   4. KoSequenceControllerのMatch Score Managerフィールドに、このスクリプトを割り当てる
///      (これだけで、KO時にラウンドスコアが更新されるようになる)
///   5. Round Announcement(RoundAnnouncementController)のInspectorで、
///      On Fight Start イベントに KoSequenceController.UnlockInputs() を追加登録する
///      (「ROUND2」「ROUND3」の表示が終わって操作が解禁されるタイミングに合わせるため)
///   6. On Round Continue イベントに、次ラウンド開始前にやりたい処理
///      (例: FighterHealthの体力リセット、キャラクターの立ち位置リセットなど)を登録する
///   7. On Match Over イベントに、試合が決着した時にやりたい追加処理があれば登録する
///      (リザルトパネルの表示自体はKoSequenceController側がそのまま行うので、
///       ここは空でもよい)
/// </summary>
public class MatchScoreManager : MonoBehaviour
{
    public enum RoundResult { Player1Win, Player2Win, Draw }

    [Header("勝利条件")]
    [Tooltip("何ラウンド先取したら試合終了か(スト6方式なら2)")]
    [SerializeField] private int roundsToWin = 2;

    [Tooltip("安全装置用の上限ラウンド数。引き分け(相打ちや時間切れの同体力)が" +
             "連続した場合でも無限にラウンドが続かないようにするためのもの。" +
             "通常のプレイでは2ラウンド目か3ラウンド目で必ず決着するので、" +
             "ここに到達するのは引き分けが複数回続いた場合のみ")]
    [SerializeField] private int maxRounds = 5;

    [Header("スコア表示")]
    [Tooltip("画面中央上部などに置く、スコア表示用のTMP_Text")]
    [SerializeField] private TMP_Text scoreText;

    [Tooltip("{0}=P1のラウンド取得数, {1}=P2のラウンド取得数")]
    [SerializeField] private string scoreFormat = "P1 {0} - {1} P2";

    [Header("次ラウンド開始")]
    [Tooltip("次ラウンドの「ROUND2」「ROUND3」演出を再生するために使うBattleStartController")]
    [SerializeField] private BattleStartController battleStartController;

    [Header("ファイター状態リセット")]
    [SerializeField]
    private FighterStateMachine player1StateMachine;

    [SerializeField]
    private FighterStateMachine player2StateMachine;


    [Header("イベント")]
    [Tooltip("ラウンドの勝敗(または引き分け)が決まったが、まだ試合全体の決着はついていない時に呼ばれる。" +
             "次ラウンドのROUND演出が再生される直前のタイミング。" +
             "体力・立ち位置のリセットなどをここに登録する")]
    [SerializeField] private UnityEvent onRoundContinue;

    [Tooltip("試合全体が決着した時に呼ばれる(2ラウンド先取、または引き分けによる安全終了)。" +
             "リザルト表示はKoSequenceController側で行われるので、" +
             "それ以外に追加でやりたい処理があればここに登録する")]
    [SerializeField] private UnityEvent onMatchOver;

    public int Player1Score { get; private set; }
    public int Player2Score { get; private set; }
    public int CurrentRound { get; private set; } = 1;
    public bool MatchIsOver { get; private set; }
    public bool Player1WonMatch { get; private set; }
    public bool MatchEndedInDraw { get; private set; }

    private void OnEnable()
    {
        ResetMatch();
    }

    /// <summary>
    /// 試合全体のスコアを最初からやり直す(再戦する場合などに呼ぶ)。
    /// </summary>
    public void ResetMatch()
    {
        Player1Score = 0;
        Player2Score = 0;
        CurrentRound = 1;
        MatchIsOver = false;
        MatchEndedInDraw = false;
        UpdateScoreText();
    }

    /// <summary>
    /// このラウンド結果を登録したら試合が決着するか、を(登録せずに)調べる。
    /// 「試合が続くなら暗転してからリセットする」といった事前判断に使う。
    /// </summary>
    public bool WouldEndMatch(RoundResult result)
    {
        int p1 = Player1Score + (result == RoundResult.Player1Win ? 1 : 0);
        int p2 = Player2Score + (result == RoundResult.Player2Win ? 1 : 0);

        return p1 >= roundsToWin || p2 >= roundsToWin || CurrentRound >= maxRounds;
    }

    /// <summary>
    /// 1ラウンド分の結果を登録する。KoSequenceControllerがKOを検知したタイミングで呼ぶ想定。
    /// </summary>
    public void RegisterRoundResult(RoundResult result)
    {
        if (MatchIsOver) return;

        if (result == RoundResult.Player1Win) Player1Score++;
        else if (result == RoundResult.Player2Win) Player2Score++;
        // Draw: どちらのスコアも加算しない(次ラウンドへ)

        UpdateScoreText();

        if (Player1Score >= roundsToWin || Player2Score >= roundsToWin)
        {
            FinishMatch(Player1Score > Player2Score, isDraw: false);
            return;
        }

        if (CurrentRound >= maxRounds)
        {
            // 安全装置: 引き分けが続いてラウンド数の上限に達した場合
            bool scoreIsTied = Player1Score == Player2Score;
            FinishMatch(Player1Score > Player2Score, isDraw: scoreIsTied);
            return;
        }

        // まだ決着していない → 次のラウンドへ
        CurrentRound++;

        // 死亡時のKnockDown状態を解除
        if (player1StateMachine != null)
        {
            player1StateMachine.TryChangeState(
                FighterState.Idle
            );
        }

        if (player2StateMachine != null)
        {
            player2StateMachine.TryChangeState(
                FighterState.Idle
            );
        }

        // HP・位置など既存のリセット処理
        onRoundContinue?.Invoke();

        // ROUND2 / ROUND3演出
        if (battleStartController != null)
        {
            battleStartController.PlayRound(
                CurrentRound
            );
        }

    }

    private void FinishMatch(bool player1Won, bool isDraw)
    {
        MatchIsOver = true;
        Player1WonMatch = player1Won;
        MatchEndedInDraw = isDraw;
        onMatchOver?.Invoke();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, Player1Score, Player2Score);
        }
    }
}