using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameModePanel : MonoBehaviour
{
    [Header("選択項目")]
    [SerializeField] private TMP_Text[] menuTexts;

    [Header("カラー")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectColor = Color.red;
    [SerializeField] private GameObject characterRoot;
    [Header("遷移時間")]
    [SerializeField] private float panelFadeDuration = 0.2f; // ← キャラ選択⇔モード変更(今のまま変更しなくてよい想定だが手動調整可能に)

    [Header("SE")]
    [SerializeField] private AudioClip moveSe;   // カーソル移動音
    [SerializeField] private AudioClip decideSe; // 決定音
    [SerializeField] private AudioClip cancelSe; // 戻る音(MainMenuへ)

    [Header("キーボード操作")]
    [Tooltip("移動キー(上)")]
    [SerializeField] private Key keyboardUpKey = Key.UpArrow;
    [Tooltip("移動キー(下)")]
    [SerializeField] private Key keyboardDownKey = Key.DownArrow;
    [Tooltip("決定キー(複数指定可)")]
    [SerializeField] private Key[] keyboardDecideKeys = { Key.A, Key.Enter, Key.Space };

    private int currentIndex = 0;
    private bool decided = false;
    private Gamepad player1Pad;
    [SerializeField] private CharacterSelectManager characterManager;
    [SerializeField]
    private string menuSceneName = "MainMenu";
    private bool changingScene = false;

    public void BackToMainMenu()
    {
        if (changingScene)
            return;

        changingScene = true;
        decided = true;

        Debug.Log("モード選択 → MainMenu");

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeToScene(menuSceneName);
        }
        else
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }

    private void OnEnable()
    {
        changingScene = false;
        Initialize();
    }

    private void Update()
    {
        //if (player1Pad != null)
        //{
        //    Debug.Log(player1Pad.buttonEast.wasPressedThisFrame);
        //}
        if (!enabled || changingScene)
            return;

        player1Pad = Gamepad.current;

        // 戻る(キーボードはEscape、ゲームパッドはBボタン)
        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            (player1Pad != null &&
                player1Pad.buttonEast.wasPressedThisFrame))
        {
            Debug.Log("GameModePanel : B");
            PlaySe(cancelSe);
            BackToMainMenu();
            return;
        }

        if (decided)
            return;

        bool up = IsUpPressed();
        bool down = IsDownPressed();
        bool submit = IsDecidePressed();

        if (up)
        {
            currentIndex--;

            if (currentIndex < 0)
                currentIndex = menuTexts.Length - 1;

            PlaySe(moveSe);
            UpdateSelection();
        }

        if (down)
        {
            currentIndex++;

            if (currentIndex >= menuTexts.Length)
                currentIndex = 0;

            PlaySe(moveSe);
            UpdateSelection();
        }

        if (submit)
        {
            PlaySe(decideSe);
            Decide();
        }
    }

    private bool IsUpPressed()
    {
        bool key = Keyboard.current != null && Keyboard.current[keyboardUpKey].wasPressedThisFrame;
        bool gp = player1Pad != null && player1Pad.dpad.up.wasPressedThisFrame;
        return key || gp;
    }

    private bool IsDownPressed()
    {
        bool key = Keyboard.current != null && Keyboard.current[keyboardDownKey].wasPressedThisFrame;
        bool gp = player1Pad != null && player1Pad.dpad.down.wasPressedThisFrame;
        return key || gp;
    }

    private bool IsDecidePressed()
    {
        bool gp = player1Pad != null && player1Pad.buttonSouth.wasPressedThisFrame;
        return IsKeyboardDecidePressed() || gp;
    }

    private bool IsKeyboardDecidePressed()
    {
        if (Keyboard.current == null) return false;

        for (int i = 0; i < keyboardDecideKeys.Length; i++)
        {
            if (Keyboard.current[keyboardDecideKeys[i]].wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private void PlaySe(AudioClip clip)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            menuTexts[i].color =
                (i == currentIndex) ? selectColor : normalColor;
        }
    }

    private void Decide()
    {
        StartCoroutine(DecideRoutine());
    }
    private IEnumerator DecideRoutine()
    {
        decided = true;

        GameModeManager.Instance.CurrentMode =
            (currentIndex == 0)
            ? GameModeManager.Mode.PlayerVsPlayer
            : GameModeManager.Mode.PlayerVsCPU;

        // フェードアウト
        yield return FadeManager.Instance.StartFadeOut(panelFadeDuration);

        // 初期化
        characterManager.Initialize();

        // パネル切り替え
        characterRoot.SetActive(true);
        gameObject.SetActive(false);

        // フェードイン
        FadeManager.Instance.StartFadeIn(panelFadeDuration);
    }
    private IEnumerator BackRoutine()
    {
        // フェードアウト
        yield return FadeManager.Instance.StartFadeOut(0.5f);

        // メインメニューへ
        SceneManager.LoadScene(menuSceneName);
    }
    public void Initialize()
    {
        decided = false;
        changingScene = false;
        currentIndex = 0;

        UpdateSelection();

#if UNITY_EDITOR
        Debug.Log("GameModePanel Initialize");
#endif
    }
}