using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuManager : MonoBehaviour
{
    [Header("メインメニュー")]
    [SerializeField] private TMP_Text[] menuTexts;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private string nextScene = "CharacterSelection";

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
            (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (submit)
        {
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
            (Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame);
        bool right = Keyboard.current.rightArrowKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame);

        if (left)
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = menuTexts.Length - 1;
            UpdateSelection();
        }

        if (right)
        {
            currentIndex++;
            if (currentIndex >= menuTexts.Length)
                currentIndex = 0;
            UpdateSelection();
        }
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            if (i == currentIndex)
            {
                menuTexts[i].color = Color.red;
                menuTexts[i].fontSize = 48;
            }
            else
            {
                menuTexts[i].color = Color.white;
                menuTexts[i].fontSize = 40;
            }
        }
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