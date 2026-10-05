using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// どちらかがKOされたときの演出を管理する。
///
///   KO発生
///    → スローモーション(slowDuration秒 ※実時間)
///    → 通常速度に戻して「K.O」を画面中央に表示(ポップ演出つき)
///    → koDisplayDuration秒後、
///         MatchScoreManagerが設定されていればラウンド結果を登録し、
///           ・まだ試合が決着していなければ次ラウンド(ROUND2/3)を開始
///           ・2ラウンド先取していれば、同じシーン内のリザルトパネルを表示
///         MatchScoreManagerが未設定なら、従来通りKOで即リザルトパネルを表示
///
/// 相打ち(両者が同じタイミングでKO)は、1フレームだけ待って両方のKOイベントが
/// 来ていないか確認することで検出し、引き分け(Draw)としてMatchScoreManagerに登録する。
///
/// 注意: このスクリプトは「K.O表示用オブジェクト(koRoot)」や「リザルトパネル」とは別の、
///       常にアクティブなオブジェクト(例: KoSequenceManagerなど)に付けること。
/// </summary>
public class KoSequenceController : MonoBehaviour
{
    [Header("対象キャラクター")]
    [SerializeField] private FighterHealth player1Health;
    [SerializeField] private FighterHealth player2Health;

    [Tooltip("KO演出中に操作を受け付けなくしたいキャラクターのFighterController(任意)")]
    [SerializeField] private FighterController[] controllersToLock;

    [Header("KO時の動き停止")]
    [Tooltip("KOの瞬間に横方向の移動を止めたいRigidbody2D(P1/P2)。" +
             "横の速度を0にして横移動をロックする(縦方向は止めないので、倒れる/落下はそのまま)。" +
             "次ラウンドの開始時(UnlockInputs)に元へ戻る")]
    [SerializeField] private Rigidbody2D[] bodiesToFreeze;
    [Tooltip("KOの瞬間に無効化したいコンポーネント(CPUのAI、移動スクリプトなど)。" +
             "FighterControllerとは別に勝手に動かしているスクリプトがあればここに入れる。" +
             "次ラウンドの開始時(UnlockInputs)に再び有効になる")]
    [SerializeField] private Behaviour[] componentsToDisable;

    [Header("KO時に消す表示")]
    [Tooltip("KOの瞬間に消したいコンボ表示(Player1側・Player2側のComboCounterUI)")]
    [SerializeField] private ComboCounterUI[] comboCountersToHide;

    [Header("スローモーション")]
    [Tooltip("スロー中のTime.timeScale(小さいほど遅い)")]
    [SerializeField, Range(0.05f, 1f)] private float slowTimeScale = 0.25f;
    [Tooltip("スローモーションを続ける時間(秒)。実時間で数える")]
    [SerializeField, Min(0f)] private float slowDuration = 2f;

    [Header("K.O表示")]
    [Tooltip("画面中央に表示するK.O(TextMeshProやImageの親オブジェクト)。最初は非表示にしておく")]
    [SerializeField] private GameObject koRoot;
    [SerializeField] private float koPopScale = 2.5f;
    [SerializeField, Min(0.01f)] private float koPopDuration = 0.25f;
    [Tooltip("K.O表示後、次の処理(次ラウンド開始 or リザルト表示)に進むまでの時間(秒)")]
    [SerializeField, Min(0f)] private float koDisplayDuration = 2f;
    [Tooltip("ONにすると、次のラウンドへ進む時に暗転(フェードアウト)→リセット→明転(フェードイン)する。" +
             "キャラの位置や体力のリセットが画面に映らなくなる。FadeManagerが必要")]
    [SerializeField] private bool fadeBetweenRounds = true;
    [Tooltip("ラウンド間のフェードアウト/フェードインそれぞれの時間(秒)")]
    [SerializeField, Min(0.05f)] private float roundFadeDuration = 0.4f;
    [Tooltip("真っ暗な状態を保つ時間(秒)。この間にリセットとROUND演出の準備が行われる")]
    [SerializeField, Min(0f)] private float roundBlackHoldDuration = 0.3f;
    [Tooltip("FadeManagerがシーンに無い時(このシーンを直接再生している時など)の代わりに使う、" +
             "全画面の黒いImageに付けたCanvasGroup(任意)。最前面に置き、Alpha=0・Blocks Raycastsオフにしておく")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [Tooltip("ONにすると、ラウンド1のKOではK.O表示(とそのSE)を出さない。" +
             "スローモーションと待ち時間はそのまま。Match Score Managerが設定されている場合のみ有効")]
    [SerializeField] private bool hideKoInRound1 = true;

    [Header("ラウンド制スコア(任意)")]
    [Tooltip("設定すると、KOごとにラウンド結果(P1勝ち/P2勝ち/引き分け)を登録し、" +
             "2ラウンド先取/1-1ならラウンド3まで、というスコア制で試合を進行する。" +
             "未設定の場合は従来通り、KO即リザルト表示の挙動になる")]
    [SerializeField] private MatchScoreManager matchScoreManager;

    [Header("リザルト(同じシーン内のパネル)")]
    [Tooltip("表示するリザルトパネル(ResultPanelなど)。最初は非表示のままでよい")]
    [SerializeField] private GameObject resultPanel;
    [Tooltip("リザルトパネルを表示する時に非表示にしたいオブジェクト(HPバー、スコアテキストなど)。" +
             "何個でも追加できる。見た目だけのオブジェクトを入れること" +
             "(このスクリプトやMatchScoreManagerが付いたオブジェクト自体は入れない)")]
    [SerializeField] private GameObject[] hideOnResult;
    [Tooltip("勝者を表示するテキスト(任意)。ResultTextなど")]
    [SerializeField] private TMP_Text resultText;
    [Tooltip("勝者テキストの書式。{0}に勝者名が入る")]
    [SerializeField] private string winnerFormat = "{0} WIN!";
    [Tooltip("試合全体が引き分けで終わった場合に表示するテキスト" +
             "(MatchScoreManagerの安全装置で、引き分けが続いた末に同点で終了した場合のみ使われる)")]
    [SerializeField] private string drawText = "DRAW";
    [SerializeField] private string player1Name = "PLAYER 1";
    [SerializeField] private string player2Name = "PLAYER 2";
    [Tooltip("CPU戦のとき、Player2側の名前として使う")]
    [SerializeField] private string cpuName = "CPU";
    [Tooltip("パネルにCanvasGroupが付いていれば、この時間でフェードインする(秒)")]
    [SerializeField, Min(0f)] private float resultFadeDuration = 0.4f;
    [Tooltip("リザルト表示時に最初に選択状態にするボタン(ゲームパッド操作用、任意)")]
    [SerializeField] private GameObject firstSelectedButton;

    [Tooltip("リザルト表示のタイミングで追加で呼びたい処理があれば登録する(任意)")]
    [SerializeField] private UnityEvent onShowResult;

    [Header("SE")]
    [SerializeField] private AudioClip koSe;

    /// <summary>KOシーケンスが始まっているか</summary>
    public bool IsRunning { get; private set; }

    /// <summary>このラウンドで勝ったのがPlayer1側か(引き分けの場合は意味を持たない)</summary>
    public bool Player1Won { get; private set; }

    /// <summary>このラウンドが引き分け(相打ちなど)だったか</summary>
    public bool RoundWasDraw { get; private set; }

    private bool timeScaleChanged;

    // 相打ち(両者同時KO)判定用
    private bool? firstKnockoutIsPlayer1;
    private bool doubleKnockoutDetected;

    private void OnEnable()
    {
        if (player1Health != null) player1Health.OnKnockedOut += HandlePlayer1KnockedOut;
        if (player2Health != null) player2Health.OnKnockedOut += HandlePlayer2KnockedOut;

        // 演出の表示物は最初は隠しておく
        if (koRoot != null) koRoot.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void OnDisable()
    {
        if (player1Health != null) player1Health.OnKnockedOut -= HandlePlayer1KnockedOut;
        if (player2Health != null) player2Health.OnKnockedOut -= HandlePlayer2KnockedOut;

        RestoreTimeScale();
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
    }

    private void HandlePlayer1KnockedOut()
    {
        // Player1が倒された
        RegisterKnockout(player1IsLoser: true);
    }

    private void HandlePlayer2KnockedOut()
    {
        RegisterKnockout(player1IsLoser: false);
    }

    private void RegisterKnockout(bool player1IsLoser)
    {
        if (IsRunning) return;

        if (firstKnockoutIsPlayer1 == null)
        {
            firstKnockoutIsPlayer1 = player1IsLoser;
            StartCoroutine(WaitForDoubleKnockoutRoutine());
        }
        else if (firstKnockoutIsPlayer1.Value != player1IsLoser)
        {
            // 既に一方のKOを受け付けた直後に、反対側のKOも来た = 相打ち(両者同時KO)
            doubleKnockoutDetected = true;
        }
    }

    /// <summary>
    /// 両者が同じタイミング(相打ち)でKOされた場合を判定するため、1フレームだけ待って
    /// もう片方のKOイベントが来ていないか確認してから、実際のシーケンスを開始する。
    /// </summary>
    private IEnumerator WaitForDoubleKnockoutRoutine()
    {
        yield return null;

        if (IsRunning) yield break;

        IsRunning = true;

        if (doubleKnockoutDetected)
        {
            RoundWasDraw = true;
            Player1Won = false;
        }
        else
        {
            RoundWasDraw = false;
            Player1Won = !firstKnockoutIsPlayer1.Value; // player1IsLoser=true → Player2の勝ち
        }

        HideComboCounters();
        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        LockInputs();

        // ---- スローモーション ----
        // 注意: Time.fixedDeltaTimeは変更しない(FixedUpdate単位のフレーム処理も一緒に遅くなるように)
        Time.timeScale = slowTimeScale;
        timeScaleChanged = true;

        yield return new WaitForSecondsRealtime(slowDuration);

        // ---- 通常速度に戻して K.O 表示 ----
        RestoreTimeScale();

        bool hideKoThisRound =
            hideKoInRound1 &&
            matchScoreManager != null &&
            matchScoreManager.CurrentRound == 1;

        if (koRoot != null && !hideKoThisRound)
        {
            koRoot.SetActive(true);

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(koSe);

            yield return PopKoRoutine();
        }

        yield return new WaitForSecondsRealtime(koDisplayDuration);

        // ---- ラウンドスコアを使う場合は、次ラウンドへ進むか試合終了かをここで判定 ----
        if (matchScoreManager != null)
        {
            MatchScoreManager.RoundResult result = RoundWasDraw
                ? MatchScoreManager.RoundResult.Draw
                : (Player1Won ? MatchScoreManager.RoundResult.Player1Win : MatchScoreManager.RoundResult.Player2Win);

            // 試合が続く場合は、リセット(位置・体力など)が見えないよう、
            // 暗転 → リセットとROUND演出の開始 → 明転 の順で進める
            bool fadeAroundReset =
                fadeBetweenRounds &&
                !matchScoreManager.WouldEndMatch(result);

            bool useFadeManager = FadeManager.Instance != null;
            bool useOverlay = !useFadeManager && fadeOverlay != null;

            Debug.Log(
                $"[KoSequenceController] ラウンド間フェード判定: Fade Between Rounds={fadeBetweenRounds}, " +
                $"試合が続く={!matchScoreManager.WouldEndMatch(result)}, " +
                $"FadeManager={(useFadeManager ? "あり" : "なし")}, " +
                $"Fade Overlay={(fadeOverlay != null ? "あり" : "なし")}, " +
                $"現在ラウンド={matchScoreManager.CurrentRound}", this);

            if (fadeAroundReset && !useFadeManager && !useOverlay)
            {
                Debug.LogWarning(
                    "[KoSequenceController] 暗転できません。FadeManagerがシーンに存在しない" +
                    "(メインメニューから開始していない)ため、Fade Overlay(CanvasGroup)を" +
                    "割り当てるか、メインメニューから起動してください。", this);
                fadeAroundReset = false;
            }

            if (fadeAroundReset)
            {
                if (useFadeManager)
                    yield return FadeManager.Instance.StartFadeOut(roundFadeDuration);
                else
                    yield return FadeOverlayRoutine(0f, 1f);

                if (koRoot != null) koRoot.SetActive(false);
            }

            matchScoreManager.RegisterRoundResult(result);

            if (fadeAroundReset)
            {
                // 真っ暗な状態を少し保つ(これが無いと、暗くなった瞬間に明るくなり始めて
                // 暗転したことに気づきにくい)
                yield return new WaitForSecondsRealtime(roundBlackHoldDuration);

                if (useFadeManager)
                    FadeManager.Instance.StartFadeIn(roundFadeDuration);
                else
                    StartCoroutine(FadeOverlayRoutine(1f, 0f));
            }

            if (matchScoreManager.MatchIsOver)
            {
                yield return ShowResultRoutine();
            }
            else
            {
                ContinueToNextRound();
            }
        }
        else
        {
            // MatchScoreManager未設定の場合は従来通り、KOで即リザルト表示
            yield return ShowResultRoutine();
        }
    }

    private IEnumerator FadeOverlayRoutine(float from, float to)
    {
        float timer = 0f;
        fadeOverlay.alpha = from;

        while (timer < roundFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeOverlay.alpha = Mathf.Lerp(from, to, timer / roundFadeDuration);
            yield return null;
        }

        fadeOverlay.alpha = to;
    }

    /// <summary>
    /// K.Oが大きい状態からドンと通常サイズに収まる演出(ease-out)。
    /// </summary>
    private IEnumerator PopKoRoutine()
    {
        Transform t = koRoot.transform;
        float timer = 0f;

        while (timer < koPopDuration)
        {
            timer += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(timer / koPopDuration);
            float eased = 1f - Mathf.Pow(1f - ratio, 3f);
            t.localScale = Vector3.one * Mathf.Lerp(koPopScale, 1f, eased);
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    /// <summary>
    /// ラウンドの決着はついたが、まだ試合全体は決着していない場合に呼ばれる。
    /// K.O表示を消し、このコンポーネントの内部状態をリセットして次ラウンドに備える。
    /// 体力・キャラクターの立ち位置のリセットは、MatchScoreManagerのOnRoundContinue
    /// イベント側で行う(このスクリプトはKOシーケンス自体の管理に専念する)。
    /// 次ラウンドの「ROUND2」「ROUND3」演出はMatchScoreManagerがBattleStartControllerを
    /// 通じて再生し、その演出が終わるタイミング(RoundAnnouncementControllerのOnFightStart)
    /// でUnlockInputs()を呼ぶようにInspectorで登録しておくこと。
    /// </summary>
    private void ContinueToNextRound()
    {
        if (koRoot != null) koRoot.SetActive(false);

        IsRunning = false;
        RoundWasDraw = false;
        firstKnockoutIsPlayer1 = null;
        doubleKnockoutDetected = false;

        // 入力は、次ラウンドのROUND演出が終わるタイミング(OnFightStart)で
        // UnlockInputs()が呼ばれるまでロックしたままにしておく
    }

    /// <summary>
    /// 次ラウンドのROUND演出が終わったタイミングで呼ぶ。LockInputs()で止めていた
    /// controllersToLockの操作を再び有効にする。
    /// RoundAnnouncementControllerのOn Fight StartイベントにこのメソッドをInspectorで
    /// 登録しておくこと(BattleStartController.ResumeGame()と並べて登録してよい)。
    /// </summary>
    public void UnlockInputs()
    {
        UnfreezeMovement();

        if (controllersToLock == null) return;

        foreach (FighterController controller in controllersToLock)
        {
            if (controller != null) controller.SetUseLocalInput(true);
        }
    }

    /// <summary>
    /// リザルトパネルを表示する。K.Oはリザルトが出る直前に非表示にする。
    /// </summary>
    private IEnumerator ShowResultRoutine()
    {
        // K.Oの文字はリザルト画面では表示しない
        if (koRoot != null)
        {
            koRoot.SetActive(false);
        }

        if (hideOnResult != null)
        {
            foreach (GameObject target in hideOnResult)
            {
                if (target != null) target.SetActive(false);
            }
        }

        if (resultText != null)
        {
            resultText.text = IsMatchDraw()
                ? drawText
                : string.Format(winnerFormat, GetWinnerName());
        }

        if (resultPanel != null)
        {
            CanvasGroup group = resultPanel.GetComponent<CanvasGroup>();

            if (group != null && resultFadeDuration > 0f)
            {
                group.alpha = 0f;
                resultPanel.SetActive(true);

                float timer = 0f;
                while (timer < resultFadeDuration)
                {
                    timer += Time.unscaledDeltaTime;
                    group.alpha = Mathf.Clamp01(timer / resultFadeDuration);
                    yield return null;
                }

                group.alpha = 1f;
            }
            else
            {
                resultPanel.SetActive(true);
            }
        }

        // ゲームパッドで操作できるよう、最初のボタンを選択状態にする
        if (firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }

        onShowResult?.Invoke();
    }

    private bool IsMatchDraw()
    {
        return matchScoreManager != null
            ? matchScoreManager.MatchEndedInDraw
            : RoundWasDraw; // MatchScoreManager未設定時は、このラウンドの結果がそのまま試合結果
    }

    private bool DidPlayer1WinMatch()
    {
        return matchScoreManager != null
            ? matchScoreManager.Player1WonMatch
            : Player1Won;
    }

    private string GetWinnerName()
    {
        if (DidPlayer1WinMatch()) return player1Name;

        bool cpuMode =
            GameModeManager.Instance != null &&
            GameModeManager.Instance.CurrentMode == GameModeManager.Mode.PlayerVsCPU;

        return cpuMode ? cpuName : player2Name;
    }

    private void HideComboCounters()
    {
        if (comboCountersToHide == null) return;

        foreach (ComboCounterUI counter in comboCountersToHide)
        {
            if (counter != null) counter.Suppress();
        }
    }

    private RigidbodyConstraints2D[] savedConstraints;

    private void LockInputs()
    {
        FreezeMovement();

        if (controllersToLock == null) return;

        foreach (FighterController controller in controllersToLock)
        {
            if (controller != null) controller.SetUseLocalInput(false);
        }
    }

    /// <summary>KO時にキャラの横移動と、勝手に動かすコンポーネントを止める。</summary>
    private void FreezeMovement()
    {
        if (bodiesToFreeze != null)
        {
            savedConstraints = new RigidbodyConstraints2D[bodiesToFreeze.Length];

            for (int i = 0; i < bodiesToFreeze.Length; i++)
            {
                Rigidbody2D body = bodiesToFreeze[i];
                if (body == null) continue;

                savedConstraints[i] = body.constraints;

                Vector2 v = body.linearVelocity;
                v.x = 0f;
                body.linearVelocity = v;
                body.angularVelocity = 0f;
                body.constraints = savedConstraints[i] | RigidbodyConstraints2D.FreezePositionX;
            }
        }

        if (componentsToDisable != null)
        {
            foreach (Behaviour component in componentsToDisable)
            {
                if (component != null) component.enabled = false;
            }
        }
    }

    private void UnfreezeMovement()
    {
        if (bodiesToFreeze != null && savedConstraints != null)
        {
            for (int i = 0; i < bodiesToFreeze.Length && i < savedConstraints.Length; i++)
            {
                if (bodiesToFreeze[i] != null) bodiesToFreeze[i].constraints = savedConstraints[i];
            }
        }

        if (componentsToDisable != null)
        {
            foreach (Behaviour component in componentsToDisable)
            {
                if (component != null) component.enabled = true;
            }
        }
    }

    private void RestoreTimeScale()
    {
        if (!timeScaleChanged) return;

        Time.timeScale = 1f;
        timeScaleChanged = false;
    }
}