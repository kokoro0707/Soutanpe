using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("遷移先シーン")]
    [SerializeField] private string nextScene = "MainMenu";

    [Header("キーボード操作")]
    [Tooltip("スタートとして扱うキーボードのキー(複数指定可)")]
    [SerializeField] private Key[] keyboardStartKeys = { Key.Enter, Key.Space };

    [Header("PCデバッグ用キー(任意、キーボード操作とは別に1つだけ追加したい場合)")]
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

        bool keyboardStart = IsKeyboardStartPressed();

        bool debugKeyStart =
            Keyboard.current != null &&
            Keyboard.current[debugStartKey].wasPressedThisFrame;

        if (gamepadStart || keyboardStart || debugKeyStart)
        {
            StartGame();
        }
    }

    /// <summary>
    /// keyboardStartKeys に登録されたキーのいずれかが押されたかを判定する。
    /// </summary>
    private bool IsKeyboardStartPressed()
    {
        if (Keyboard.current == null) return false;

        for (int i = 0; i < keyboardStartKeys.Length; i++)
        {
            if (Keyboard.current[keyboardStartKeys[i]].wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private void StartGame()
    {
        started = true;
        Debug.Log("ゲーム開始");
        FadeManager.Instance.FadeToScene(nextScene);
    }
}