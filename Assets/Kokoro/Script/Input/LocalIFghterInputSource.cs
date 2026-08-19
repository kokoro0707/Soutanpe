using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerInputから入力を取得し、
/// FighterInputDataへ変換する。
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public sealed class LocalFighterInputSource :
    MonoBehaviour,
    IFighterInputSource
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
    private string lightAttackActionName =
        "LightAttack";

    [SerializeField]
    private string heavyAttackActionName =
        "HeavyAttack";

    [SerializeField]
    private string assistComboActionName =
        "AssistCombo";

    [Header("方向入力設定")]
    [SerializeField, Range(0.1f, 0.95f)]
    private float directionPressThreshold = 0.55f;

    [SerializeField, Range(0.05f, 0.9f)]
    private float directionReleaseThreshold = 0.25f;

    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction lightAttackAction;
    private InputAction heavyAttackAction;
    private InputAction assistComboAction;

    private bool isInitialized;

    private int currentHorizontalDirection;
    private int previousVerticalDirection;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        InitializeActions();
    }

    private void Start()
    {
        if (!isInitialized)
            return;

        if (playerInput.currentActionMap == null ||
            playerInput.currentActionMap.name != actionMapName)
        {
            playerInput.SwitchCurrentActionMap(
                actionMapName
            );
        }
    }

    private void InitializeActions()
    {
        if (playerInput == null)
        {
            Debug.LogError(
                $"{name}にPlayerInputがありません。",
                this
            );

            return;
        }

        if (playerInput.actions == null)
        {
            Debug.LogError(
                $"{name}のPlayerInputにInput Actionsがありません。",
                this
            );

            return;
        }

        InputActionMap actionMap =
            playerInput.actions.FindActionMap(
                actionMapName
            );

        if (actionMap == null)
        {
            Debug.LogError(
                $"Action Map「{actionMapName}」がありません。",
                this
            );

            return;
        }

        moveAction =
            FindAction(
                actionMap,
                moveActionName
            );

        jumpAction =
            FindAction(
                actionMap,
                jumpActionName
            );

        lightAttackAction =
            FindAction(
                actionMap,
                lightAttackActionName
            );

        heavyAttackAction =
            FindAction(
                actionMap,
                heavyAttackActionName
            );

        assistComboAction =
            FindAction(
                actionMap,
                assistComboActionName
            );

        isInitialized =
            moveAction != null &&
            jumpAction != null &&
            lightAttackAction != null &&
            heavyAttackAction != null &&
            assistComboAction != null;

        if (isInitialized)
        {
            Debug.Log(
                $"{name}のInput Action読み込み成功",
                this
            );
        }
    }

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
                $"Action「{actionName}」がありません。",
                this
            );
        }

        return action;
    }

    public FighterInputData ReadInput()
    {
        if (!isInitialized)
            return default;

        Vector2 moveInput =
            moveAction.ReadValue<Vector2>();

        int horizontal =
            ConvertHorizontalWithHysteresis(
                moveInput.x
            );

        int vertical =
            ConvertVerticalToDigital(
                moveInput.y
            );

        bool upPressedThisFrame =
            vertical == 1 &&
            previousVerticalDirection != 1;

        bool jumpButtonPressed =
            jumpAction.WasPressedThisFrame();

        FighterInputData inputData =
            new FighterInputData
            {
                horizontal = horizontal,
                vertical = vertical,

                jumpPressed =
                    upPressedThisFrame ||
                    jumpButtonPressed,

                lightAttackPressed =
                    lightAttackAction
                        .WasPressedThisFrame(),

                heavyAttackPressed =
                    heavyAttackAction
                        .WasPressedThisFrame(),

                assistComboPressed =
                    assistComboAction
                        .WasPressedThisFrame()
            };

        previousVerticalDirection =
            vertical;

        return inputData;
    }

    private int ConvertHorizontalWithHysteresis(
        float value
    )
    {
        if (currentHorizontalDirection == 0)
        {
            if (value >= directionPressThreshold)
            {
                currentHorizontalDirection = 1;
            }
            else if (value <=
                     -directionPressThreshold)
            {
                currentHorizontalDirection = -1;
            }

            return currentHorizontalDirection;
        }

        if (currentHorizontalDirection == 1)
        {
            if (value <= directionReleaseThreshold)
            {
                currentHorizontalDirection = 0;
            }

            return currentHorizontalDirection;
        }

        if (value >= -directionReleaseThreshold)
        {
            currentHorizontalDirection = 0;
        }

        return currentHorizontalDirection;
    }

    private int ConvertVerticalToDigital(
        float value
    )
    {
        if (value >= directionPressThreshold)
            return 1;

        if (value <= -directionPressThreshold)
            return -1;

        return 0;
    }

    private void OnDisable()
    {
        currentHorizontalDirection = 0;
        previousVerticalDirection = 0;
    }

    private void OnValidate()
    {
        directionReleaseThreshold =
            Mathf.Min(
                directionReleaseThreshold,
                directionPressThreshold - 0.05f
            );

        directionReleaseThreshold =
            Mathf.Max(
                0.05f,
                directionReleaseThreshold
            );
    }
}
