using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("遷移先シーン")]
    [SerializeField] private string nextScene = "MainMenu";

    [Header("PCデバッグ用キー")]
    [SerializeField] private Key debugStartKey = Key.A;

    private bool started;

    private void Update()
    {
        // F1キーを押した時だけ表示
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Debug.Log("===== Gamepad Check =====");
            Debug.Log("Gamepad Count : " + Gamepad.all.Count);
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad pad = Gamepad.all[i];
                Debug.Log(
                    $"[{i}] Name:{pad.displayName}  ID:{pad.deviceId}  Interface:{pad.description.interfaceName}"
                );
            }
        }

        if (started)
            return;

        bool gamepadStart =
            Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame;

        bool debugKeyStart =
            Keyboard.current != null &&
            Keyboard.current[debugStartKey].wasPressedThisFrame;

        if (gamepadStart || debugKeyStart)
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