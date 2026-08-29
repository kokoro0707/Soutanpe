using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ローカル2人対戦で使用するデバイスを割り当てる。
/// ・キーボード＋ゲームパッド
/// ・ゲームパッド2台
/// ・キーボード＋キーボード
/// </summary>
public sealed class LocalTwoPlayerDeviceSetup : MonoBehaviour
{
    public enum LocalDeviceMode
    {
        KeyboardAndGamepad,
        TwoGamepads,
        KeyboardAndKeyboard
    }

    [Header("入力方式")]
    [SerializeField]
    private LocalDeviceMode deviceMode =
        LocalDeviceMode.KeyboardAndKeyboard;

    [Header("プレイヤー")]
    [SerializeField]
    private PlayerInput player1Input;

    [SerializeField]
    private PlayerInput player2Input;

    [Header("Control Scheme名")]
    [SerializeField]
    private string keyboardSchemeName = "Keyboard";

    [SerializeField]
    private string gamepadSchemeName = "Gamepad";

    [Header("Action Map名")]
    [SerializeField]
    private string actionMapName = "Fighter";

    private void Start()
    {
        if (!ValidatePlayerInputs())
        {
            return;
        }

        switch (deviceMode)
        {
            case LocalDeviceMode.KeyboardAndGamepad:
                SetupKeyboardAndGamepad();
                break;

            case LocalDeviceMode.TwoGamepads:
                SetupTwoGamepads();
                break;

            case LocalDeviceMode.KeyboardAndKeyboard:
                SetupKeyboardAndKeyboard();
                break;
        }
    }

    /// <summary>
    /// Player1をゲームパッド、
    /// Player2をキーボードに設定する。
    /// </summary>
    private void SetupKeyboardAndGamepad()
    {
        if (Keyboard.current == null)
        {
            Debug.LogError("キーボードが見つかりません。", this);
            return;
        }

        if (Gamepad.all.Count < 1)
        {
            Debug.LogError("ゲームパッドが接続されていません。", this);
            return;
        }

        AssignDevice(
            player1Input,
            gamepadSchemeName,
            Gamepad.all[0]
        );

        AssignDevice(
            player2Input,
            keyboardSchemeName,
            Keyboard.current
        );

        Debug.Log(
            "Player1：ゲームパッド / Player2：キーボード",
            this
        );
    }

    /// <summary>
    /// Player1とPlayer2へ別々のゲームパッドを割り当てる。
    /// </summary>
    private void SetupTwoGamepads()
    {
        if (Gamepad.all.Count < 2)
        {
            Debug.LogError(
                $"ゲームパッドが2台必要です。現在の接続数：{Gamepad.all.Count}",
                this
            );

            return;
        }

        Gamepad player1Gamepad = Gamepad.all[0];
        Gamepad player2Gamepad = Gamepad.all[1];

        AssignDevice(
            player1Input,
            gamepadSchemeName,
            player1Gamepad
        );

        AssignDevice(
            player2Input,
            gamepadSchemeName,
            player2Gamepad
        );

        Debug.Log(
            $"Player1：{player1Gamepad.displayName} / " +
            $"Player2：{player2Gamepad.displayName}",
            this
        );
    }

    /// <summary>
    /// Player1とPlayer2の両方を同じキーボードで操作する。
    /// </summary>
    private void SetupKeyboardAndKeyboard()
    {
        if (Keyboard.current == null)
        {
            Debug.LogError(
                "キーボードが見つかりません。",
                this
            );

            return;
        }

        // P1にキーボード
        AssignDevice(
            player1Input,
            keyboardSchemeName,
            Keyboard.current
        );

        // P2にも同じキーボード
        AssignDevice(
            player2Input,
            keyboardSchemeName,
            Keyboard.current
        );

        Debug.Log(
            "Player1：キーボード / Player2：キーボード",
            this
        );
    }

    /// <summary>
    /// 指定したPlayerInputへデバイスを割り当てる。
    /// </summary>
    private void AssignDevice(
        PlayerInput playerInput,
        string controlScheme,
        InputDevice device
    )
    {
        if (playerInput == null || device == null)
        {
            return;
        }

        playerInput.SwitchCurrentControlScheme(
            controlScheme,
            device
        );

        playerInput.SwitchCurrentActionMap(
            actionMapName
        );
    }

    private bool ValidatePlayerInputs()
    {
        if (player1Input == null)
        {
            Debug.LogError(
                "Player1 Inputが未設定です。",
                this
            );

            return false;
        }

        if (player2Input == null)
        {
            Debug.LogError(
                "Player2 Inputが未設定です。",
                this
            );

            return false;
        }

        return true;
    }
}