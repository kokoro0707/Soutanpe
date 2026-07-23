using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("遷移先シーン")]
    [SerializeField] private string nextScene = "MainMenu";

    private bool started;

    private void Update()
    {
        if (started)
            return;

        // キーボード A
        if (Keyboard.current != null &&
            Keyboard.current.aKey.wasPressedThisFrame)
        {
            StartGame();
        }

        // コントローラー A（Xbox:A / PS:×）
        if (Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        started = true;

        Debug.Log("ゲーム開始");

        FadeManager.Instance.FadeToScene(nextScene);
    }
}