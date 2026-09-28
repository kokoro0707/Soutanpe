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
///    → koDisplayDuration秒後に、同じシーン内のリザルトパネルを表示
///       (勝者テキストの設定、フェードイン、ボタンの初期選択まで行う)
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
    [Tooltip("K.O表示後、リザルトを出すまでの時間(秒)")]
    [SerializeField, Min(0f)] private float koDisplayDuration = 2f;

    [Header("リザルト(同じシーン内のパネル)")]
    [Tooltip("表示するリザルトパネル(ResultPanelなど)。最初は非表示のままでよい")]
    [SerializeField] private GameObject resultPanel;
    [Tooltip("勝者を表示するテキスト(任意)。ResultTextなど")]
    [SerializeField] private TMP_Text resultText;
    [Tooltip("勝者テキストの書式。{0}に勝者名が入る")]
    [SerializeField] private string winnerFormat = "{0} WIN!";
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

    /// <summary>勝ったのがPlayer1側か</summary>
    public bool Player1Won { get; private set; }

    private bool timeScaleChanged;

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
        // Player1が倒された = Player2の勝ち
        StartSequence(player1Won: false);
    }

    private void HandlePlayer2KnockedOut()
    {
        StartSequence(player1Won: true);
    }

    private void StartSequence(bool player1Won)
    {
        // 相打ちなどで2回呼ばれても、最初の1回だけ処理する
        if (IsRunning) return;

        IsRunning = true;
        Player1Won = player1Won;

        // KOの瞬間にコンボ表示を消す(この直後のコンボ更新でも再表示されないようにする)
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

        if (koRoot != null)
        {
            koRoot.SetActive(true);

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(koSe);

            yield return PopKoRoutine();
        }

        yield return new WaitForSecondsRealtime(koDisplayDuration);

        // ---- 同じシーン内でリザルト表示 ----
        yield return ShowResultRoutine();
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
    /// リザルトパネルを表示する。K.Oはリザルトが出る直前に非表示にする。
    /// </summary>
    private IEnumerator ShowResultRoutine()
    {
        // K.Oの文字はリザルト画面では表示しない
        if (koRoot != null)
        {
            koRoot.SetActive(false);
        }

        if (resultText != null)
        {
            resultText.text = string.Format(winnerFormat, GetWinnerName());
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

    private string GetWinnerName()
    {
        if (Player1Won) return player1Name;

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

    private void LockInputs()
    {
        if (controllersToLock == null) return;

        foreach (FighterController controller in controllersToLock)
        {
            if (controller != null) controller.SetUseLocalInput(false);
        }
    }

    private void RestoreTimeScale()
    {
        if (!timeScaleChanged) return;

        Time.timeScale = 1f;
        timeScaleChanged = false;
    }
}