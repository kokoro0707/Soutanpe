using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro; // TextMeshProを使わない場合は下のTMP_Textを UnityEngine.UI.Text に変えてください

/// <summary>
/// どちらかのHPBarのHPが0になったらリザルトパネルを表示するマネージャー。
/// シーン内の空のGameObject（例: "GameResultManager"）にアタッチして使用します。
///
/// 前提: HPBar.cs に OnDepleted イベントが追加済みであること。
/// キャラクター側のスクリプトは一切変更不要（hpBar.SetHealth()を呼んでいれば自動で検知されます）。
///
/// パッド操作について:
///   MainMenuManagerと全く同じスタイル。Buttonコンポーネントは使わず、
///   ただのTMP_Text(見た目はテキストのみ)を配列で持ち、
///   Gamepad.current / Keyboard.current を直接ポーリングして選択・決定を行う。
///   十字キー(左右/上下どちらでも)、またはWASD(W/S)で選択項目を切り替え、
///   Aボタン(Gamepad.buttonSouth)で決定。
///   選択中の項目は赤色(selectedColor)、それ以外は白色(normalColor)で表示する。
/// </summary>
public class GameResultManager : MonoBehaviour
{
    [Header("Player HPBar References")]
    [Tooltip("Player1側のHPBarをアサインしてください")]
    [SerializeField] private HPBar player1HpBar;

    [Tooltip("Player2側のHPBarをアサインしてください")]
    [SerializeField] private HPBar player2HpBar;

    [Header("UI References")]
    [Tooltip("普段は非アクティブにしておくリザルトパネル")]
    [SerializeField] private GameObject resultPanel;

    [Tooltip("「Player1の勝利!」などを表示するテキスト")]
    [SerializeField] private TMP_Text resultText;

    [Header("Result Menu (index順に対応: 0=もう一度プレイ, 1=メインメニュー)")]
    [Tooltip("Buttonコンポーネントは不要。ただのTMP_Text(MainMenuManagerのmenuTextsと同じ形)を、選択させたい順番でセットしてください")]
    [SerializeField] private TMP_Text[] resultMenuTexts;

    [Header("選択演出")]
    [Tooltip("選択されていない項目の色")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("現在選択中(判定対象)の項目の色")]
    [SerializeField] private Color selectedColor = Color.red;
    [SerializeField] private float normalFontSize = 90f;
    [SerializeField] private float selectedFontSize = 108f;

    [Header("SE (任意)")]
    [SerializeField] private AudioClip moveSe;
    [SerializeField] private AudioClip decideSe;

    [Header("Options")]
    [Tooltip("リザルト表示時にTime.timeScaleを0にしてゲームを一時停止するか")]
    [SerializeField] private bool pauseOnResult = true;

    [Header("ラウンド制(任意)")]
    [Tooltip("MatchScoreManagerを割り当てると、HP0になるたびに反応せず、" +
             "試合が決着してリザルトパネルが出ている間だけ操作・SEが有効になる。" +
             "リザルト表示そのものはKoSequenceControllerが行う")]
    [SerializeField] private MatchScoreManager matchScoreManager;

    [Header("リザルト表示中に隠すもの (任意)")]
    [Tooltip("リザルト画面の間だけ非表示にするImage(例: HP横の顔アイコン、HPバーの枠など)。" +
             "Imageコンポーネントだけを無効にするので、そのオブジェクトの子(文字など)は残る。" +
             "子ごと隠したい時は、下のObjectsを使う")]
    [SerializeField] private UnityEngine.UI.Image[] hideImagesOnResult;
    [Tooltip("リザルト画面の間だけ、オブジェクトごと非表示にするもの(子もすべて隠れる)")]
    [SerializeField] private GameObject[] hideObjectsOnResult;

    [Header("スティック操作")]
    [Tooltip("ONにすると、左スティックでも項目を移動できる(左/上=前、右/下=次。キー・十字キーも従来どおり)")]
    [SerializeField] private bool useStick = true;
    [SerializeField, Range(0.1f, 0.95f)] private float stickThreshold = 0.6f;
    [SerializeField] private bool stickRepeat = true;
    [SerializeField] private float stickRepeatDelay = 0.4f;
    [SerializeField] private float stickRepeatInterval = 0.15f;

    private readonly StickNavigator stick = new StickNavigator();

    private bool resultTargetsHidden = false;
    private bool wasShownLastFrame = false;
    private bool isGameOver = false;
    private bool isChangingScene = false;
    private int currentIndex = 0;

    private void Awake()
    {
        // 開始時は必ず非表示にしておく
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (player1HpBar != null) player1HpBar.OnDepleted += HandlePlayer1Depleted;
        if (player2HpBar != null) player2HpBar.OnDepleted += HandlePlayer2Depleted;
    }

    private void OnDisable()
    {
        if (player1HpBar != null) player1HpBar.OnDepleted -= HandlePlayer1Depleted;
        if (player2HpBar != null) player2HpBar.OnDepleted -= HandlePlayer2Depleted;
    }

    private void Update()
    {
        // スティックの状態は常に更新しておく
        // (リザルト表示の瞬間に倒しっぱなしでも、勝手に項目が動かないようにするため)
        stick.Threshold = stickThreshold;
        stick.UseRepeat = stickRepeat;
        stick.RepeatDelay = stickRepeatDelay;
        stick.RepeatInterval = stickRepeatInterval;
        stick.Poll();

        // シーン遷移中は操作禁止
        if (isChangingScene)
            return;

        // リザルトパネルが表示されていない間は何もしない(=選択SE・決定SEも鳴らない)
        // ラウンド制の場合はisGameOverを使わず、パネルが実際に出ているかだけで判断する
        bool resultShown = resultPanel != null && resultPanel.activeInHierarchy;
        bool matchFinished = matchScoreManager != null ? matchScoreManager.MatchIsOver : isGameOver;

        if (!resultShown || !matchFinished)
            return;

        // リザルトが出ている間は、指定のImage/オブジェクトを隠す
        // (ラウンド制ではリザルト表示をKoSequenceControllerが行うので、ここで表示を検知して隠す)
        HideResultTargets();

        if (matchScoreManager != null && !wasShownLastFrame)
        {
            // リザルトが出た最初のフレームは、直前の攻撃入力などで決定されないよう入力を無視
            wasShownLastFrame = true;
            currentIndex = 0;
            UpdateSelection();
            return;
        }

        HandleNavigation();
        HandleSubmit();
    }

    private void HandlePlayer1Depleted() => HandleGameOver(loserIsPlayer1: true);
    private void HandlePlayer2Depleted() => HandleGameOver(loserIsPlayer1: false);

    private void HandleGameOver(bool loserIsPlayer1)
    {
        // ラウンド制(MatchScoreManager)を使う場合、リザルト表示はKoSequenceController側が
        // 試合決着時にだけ行う。ここでHP0のたびに反応すると、ラウンド途中で
        // timeScale=0やリザルト用の処理が裏で動いてしまうので何もしない
        if (matchScoreManager != null) return;

        // 両者同時にHPが0になった場合など、二重発火を防止
        if (isGameOver) return;
        isGameOver = true;

        // Time.timeScaleを0にする前に、ダメージバー(残像)のアニメーションを
        // 強制的に完了させておく。そうしないと追従アニメの途中で時間が止まり、
        // 赤いバーが変な位置で凍結して残ってしまう。
        if (player1HpBar != null) player1HpBar.SyncDamageBarInstantly();
        if (player2HpBar != null) player2HpBar.SyncDamageBarInstantly();

        if (pauseOnResult)
        {
            Time.timeScale = 0f;
        }

        string winnerName = loserIsPlayer1 ? "Player 2" : "Player 1";
        if (resultText != null)
        {
            resultText.text = $"{winnerName} の勝利!";
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        HideResultTargets();

        // パッド選択状態を初期化
        currentIndex = 0;
        UpdateSelection();
    }

    /// <summary>リザルト表示中だけ隠す対象を、まとめて非表示にする(二重実行しても害はない)。</summary>
    private void HideResultTargets()
    {
        if (resultTargetsHidden) return;
        resultTargetsHidden = true;

        if (hideImagesOnResult != null)
        {
            foreach (UnityEngine.UI.Image img in hideImagesOnResult)
            {
                if (img != null) img.enabled = false;
            }
        }

        if (hideObjectsOnResult != null)
        {
            foreach (GameObject go in hideObjectsOnResult)
            {
                if (go != null) go.SetActive(false);
            }
        }
    }

    private void HandleNavigation()
    {
        if (resultMenuTexts == null || resultMenuTexts.Length == 0) return;

        // 十字キー(右/下)、WASDのS、ゲームパッドの右/下で次の項目へ
        bool moveNext =
            (Keyboard.current != null &&
                (Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                 Keyboard.current.downArrowKey.wasPressedThisFrame ||
                 Keyboard.current.sKey.wasPressedThisFrame)) ||
            (Gamepad.current != null &&
                (Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.dpad.down.wasPressedThisFrame)) ||
            (useStick && (stick.RightPressed || stick.DownPressed));

        // 十字キー(左/上)、WASDのW、ゲームパッドの左/上で前の項目へ
        bool movePrev =
            (Keyboard.current != null &&
                (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                 Keyboard.current.upArrowKey.wasPressedThisFrame ||
                 Keyboard.current.wKey.wasPressedThisFrame)) ||
            (Gamepad.current != null &&
                (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.dpad.up.wasPressedThisFrame)) ||
            (useStick && (stick.LeftPressed || stick.UpPressed));

        if (moveNext)
        {
            currentIndex = (currentIndex + 1) % resultMenuTexts.Length;
            PlaySe(moveSe);
            UpdateSelection();
        }
        else if (movePrev)
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = resultMenuTexts.Length - 1;
            PlaySe(moveSe);
            UpdateSelection();
        }
    }

    private void HandleSubmit()
    {
        bool submit =
            (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame); // Xboxパッドの A ボタン

        if (submit)
        {
            PlaySe(decideSe);
            Execute();
        }
    }

    private void UpdateSelection()
    {
        if (resultMenuTexts == null) return;

        for (int i = 0; i < resultMenuTexts.Length; i++)
        {
            if (resultMenuTexts[i] == null) continue;

            if (i == currentIndex)
            {
                resultMenuTexts[i].color = selectedColor;
                resultMenuTexts[i].fontSize = selectedFontSize;
            }
            else
            {
                resultMenuTexts[i].color = normalColor;
                resultMenuTexts[i].fontSize = normalFontSize;
            }
        }
    }

    private void PlaySe(AudioClip clip)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }

    /// <summary>
    /// 現在選択中(判定対象)の項目に応じた処理を実行する。
    /// index 0 = もう一度プレイ, index 1 = メインメニューへ、という前提。
    /// resultMenuTexts の並び順を変えた場合はここも合わせて調整してください。
    /// </summary>
    private void Execute()
    {
        switch (currentIndex)
        {
            case 0:
                Retry();
                break;
            case 1:
                BackToMenu();
                break;
        }
    }

    /// <summary>もう一度対戦: 現在のシーンをリロード</summary>
    private void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>メインメニューに戻る: シーン名は実際のものに変更してください</summary>
    private void BackToMenu()
    {
        if (isChangingScene)
            return;

        isChangingScene = true;

        Time.timeScale = 1f;

        Debug.Log("Result → MainMenu");

        if (FadeManager.Instance != null)
        {
            Debug.Log("FadeManagerあり → Fade開始");

            FadeManager.Instance.FadeToScene("MainMenu");
        }
        else
        {
            Debug.LogError("FadeManagerが見つからない(気にしないでね)");

            SceneManager.LoadScene("MainMenu");
        }
    }
}