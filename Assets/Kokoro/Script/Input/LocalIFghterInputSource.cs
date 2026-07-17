using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerInputからキーボード・ゲームパッド入力を取得し、
/// FighterInputDataへ変換する。
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public sealed class LocalFighterInputSource
    : MonoBehaviour, IFighterInputSource
{
    [Header("Action Map名")]
    [SerializeField]
    private string actionMapName = "Fighter";

    [Header("Action名")]
    [SerializeField]
    private string moveActionName = "Move";

    [SerializeField]
    private string jumpActionName = "Jump";

    [SerializeField]
    private string lightAttackActionName = "LightAttack";

    [SerializeField]
    private string heavyAttackActionName = "HeavyAttack";

    [SerializeField]
    private string guardActionName = "Guard";

    [Header("方向入力設定")]
    [Tooltip("スティックをこの値以上倒すと方向入力として扱う")]
    [SerializeField, Range(0.1f, 0.95f)]
    private float directionThreshold = 0.5f;

    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction lightAttackAction;
    private InputAction heavyAttackAction;

    private bool isInitialized;

    // 前回読み取った上下方向。
    // 上入力を押した瞬間だけ検出するために使用する。
    private int previousVertical;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        InitializeActions();
    }

    private void Start()
    {
        if (!isInitialized)
        {
            return;
        }

        // 使用するAction MapをFighterに統一する
        if (playerInput.currentActionMap == null ||
            playerInput.currentActionMap.name != actionMapName)
        {
            playerInput.SwitchCurrentActionMap(actionMapName);
        }
    }

    /// <summary>
    /// PlayerInputから必要なInputActionを取得する。
    /// </summary>
    private void InitializeActions()
    {
        if (playerInput.actions == null)
        {
            Debug.LogError(
                $"{name}のPlayer InputにActionsが設定されていません。",
                this
            );

            return;
        }

        InputActionMap actionMap =
            playerInput.actions.FindActionMap(actionMapName);

        if (actionMap == null)
        {
            Debug.LogError(
                $"Action Map「{actionMapName}」が見つかりません。",
                this
            );

            return;
        }

        moveAction = FindAction(
            actionMap,
            moveActionName
        );

        jumpAction = FindAction(
            actionMap,
            jumpActionName
        );

        lightAttackAction = FindAction(
            actionMap,
            lightAttackActionName
        );

        heavyAttackAction = FindAction(
            actionMap,
            heavyAttackActionName
        );

        isInitialized =
            moveAction != null &&
            jumpAction != null &&
            lightAttackAction != null &&
            heavyAttackAction != null;

        if (isInitialized)
        {
            Debug.Log(
                $"{name}のInput Action読み込み成功",
                this
            );
        }
    }

    /// <summary>
    /// 指定した名前のActionを取得する。
    /// 見つからない場合はConsoleにエラーを表示する。
    /// </summary>
    private InputAction FindAction(
        InputActionMap actionMap,
        string actionName
    )
    {
        InputAction action =
            actionMap.FindAction(actionName);

        if (action == null)
        {
            Debug.LogError(
                $"Action「{actionName}」が見つかりません。",
                this
            );
        }

        return action;
    }

    /// <summary>
    /// 現在の入力を1フレーム分のデータとして返す。
    /// </summary>
    public FighterInputData ReadInput()
    {
        if (!isInitialized)
        {
            return default;
        }

        Vector2 moveInput =
            moveAction.ReadValue<Vector2>();

        int horizontal =
            ConvertToDigital(moveInput.x);

        int vertical =
            ConvertToDigital(moveInput.y);

        // 上方向へ入力が切り替わった瞬間を検出する。
        // 押し続けている間は再度ジャンプしない。
        bool upPressedThisFrame =
            vertical == 1 &&
            previousVertical != 1;

        // Spaceまたはゲームパッドのジャンプボタン
        bool jumpButtonPressed =
            jumpAction.WasPressedThisFrame();

        bool jumpPressed =
            upPressedThisFrame ||
            jumpButtonPressed;

        FighterInputData inputData =
            new FighterInputData
            {
                horizontal = horizontal,
                vertical = vertical,

                jumpPressed = jumpPressed,

                lightAttackPressed =
                    lightAttackAction.WasPressedThisFrame(),

                heavyAttackPressed =
                    heavyAttackAction.WasPressedThisFrame(),
            };

        // 次のフレームで比較するため保存する
        previousVertical = vertical;

        return inputData;
    }

    /// <summary>
    /// スティックの小数入力を格ゲー用の
    /// -1、0、1へ変換する。
    /// </summary>
    private int ConvertToDigital(float value)
    {
        if (value >= directionThreshold)
        {
            return 1;
        }

        if (value <= -directionThreshold)
        {
            return -1;
        }

        return 0;
    }
}
