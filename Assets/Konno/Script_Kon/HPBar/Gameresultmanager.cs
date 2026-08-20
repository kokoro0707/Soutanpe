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
///   十字キー(左右/上下どちらでも)で選択項目を切り替え、Aボタン(Gamepad.buttonSouth)で決定。
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
        // シーン遷移中は操作禁止
        if (isChangingScene)
            return;

        // リザルトパネルが表示されていない間は何もしない
        if (!isGameOver || resultPanel == null || !resultPanel.activeSelf)
            return;

        HandleNavigation();
        HandleSubmit();
    }

    private void HandlePlayer1Depleted() => HandleGameOver(loserIsPlayer1: true);
    private void HandlePlayer2Depleted() => HandleGameOver(loserIsPlayer1: false);

    private void HandleGameOver(bool loserIsPlayer1)
    {
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

        // パッド選択状態を初期化
        currentIndex = 0;
        UpdateSelection();
    }

    private void HandleNavigation()
    {
        if (resultMenuTexts == null || resultMenuTexts.Length == 0) return;

        bool moveNext =
            (Keyboard.current != null &&
                (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)) ||
            (Gamepad.current != null &&
                (Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.dpad.down.wasPressedThisFrame));

        bool movePrev =
            (Keyboard.current != null &&
                (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)) ||
            (Gamepad.current != null &&
                (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.dpad.up.wasPressedThisFrame));

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
            Debug.LogError("FadeManagerが見つからない");

            SceneManager.LoadScene("MainMenu");
        }
    }
}