using System.Collections;
using PersonaMenuUI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class MainMenuManager : MonoBehaviour
{
    [Header("メインメニュー")]
    [SerializeField] private TMP_Text[] menuTexts;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private string nextScene = "CharacterSelection";
    [Header("斜めカーソル")]
    [Tooltip("SlantedRectで作った斜めカーソルを制御するコンポーネント。未設定でも動作する(その場合はカーソル演出なし)。")]
    [SerializeField] private MenuCursorSelector cursor;
    [Header("キーボード操作")]
    [Tooltip("決定として扱うキーボードのキー(複数指定可)")]
    [SerializeField] private Key[] keyboardDecideKeys = { Key.A, Key.Enter, Key.Space };
    [Header("SE")]
    [SerializeField] private AudioClip moveSe;
    [SerializeField] private AudioClip decideSe;
    private int currentIndex = 0;
    private bool inputLock;
    private void Start()
    {
        UpdateSelection();
    }
    private void Update()
    {
        Move();
        // 設定パネルが開いている間(inputLock中)は、
        // メインメニュー側の決定操作を一切受け付けない
        if (inputLock) return;
        bool submit =
            IsKeyboardDecidePressed() ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (submit)
        {
            PlaySe(decideSe);
            Execute();
        }
    }
    private void OpenSettings()
    {
        inputLock = true;
        settingsPanel.SetActive(true);
    }
    /// <summary>
    /// 設定パネル側(SettingsNavigatorのOn Closedイベント)から呼ばれる。
    /// メインメニューの操作を再開する。
    /// パネル自体の非表示化はSettingsNavigator.ClosePanel()側で行っている。
    /// </summary>
    public void CloseSettings()
    {
        inputLock = false;
    }
    private void Move()
    {
        if (inputLock) return;
        bool left = Keyboard.current.leftArrowKey.wasPressedThisFrame ||
            Keyboard.current.aKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame);
        bool right = Keyboard.current.rightArrowKey.wasPressedThisFrame ||
            Keyboard.current.dKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame);
        if (left)
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = menuTexts.Length - 1;
            PlaySe(moveSe);
            UpdateSelection();
        }
        if (right)
        {
            currentIndex++;
            if (currentIndex >= menuTexts.Length)
                currentIndex = 0;
            PlaySe(moveSe);
            UpdateSelection();
        }
    }
    /// <summary>
    /// keyboardDecideKeys に登録されたキーのいずれかが押されたかを判定する。
    /// </summary>
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
            if (i == currentIndex)
            {
                menuTexts[i].color = Color.black;
                menuTexts[i].fontSize = 48;
            }
            else
            {
                menuTexts[i].color = Color.white;
                menuTexts[i].fontSize = 40;
            }
        }

        // 斜めカーソルを現在の選択位置へ移動させる。
        // MenuCursorSelector側の Use Internal Input / Follow Event System Selection は
        // OFFにしておき、選択の主導権はこのMainMenuManagerが持つ。
        if (cursor != null) cursor.Select(currentIndex);
    }
    private void Execute()
    {
        switch (currentIndex)
        {
            case 0:
                inputLock = true;
                StartCoroutine(StartGameRoutine());
                break;
            case 1:
                OpenSettings();
                break;
            case 2:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }
    private IEnumerator StartGameRoutine()
    {
        // タイトルと同じフェードアウト
        yield return FadeManager.Instance.StartFadeOut();
        // シーン切替
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
    }
}