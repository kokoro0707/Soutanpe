using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerInputからキーボード・ゲームパッドの入力を取得し、
/// ゲーム内部で使用するFighterInputDataへ変換する。
///
/// このクラスでは移動や攻撃を実行せず、
/// 入力情報の取得と変換だけを担当する。
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

    [Header("方向入力設定")]
    [Tooltip("スティックをこの値以上倒すと方向入力になる")]
    [SerializeField, Range(0.1f, 0.95f)]
    private float directionPressThreshold = 0.55f;

    [Tooltip("スティックをこの値未満まで戻すとニュートラルになる")]
    [SerializeField, Range(0.05f, 0.9f)]
    private float directionReleaseThreshold = 0.25f;

    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction lightAttackAction;
    private InputAction heavyAttackAction;

    private bool isInitialized;

    // 現在確定している横方向
    // 左：-1、中央：0、右：1
    private int currentHorizontalDirection;

    // 前回の上下方向
    // 上入力を押した瞬間だけジャンプさせるために使用する
    private int previousVerticalDirection;

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

        // PlayerInputで別のAction Mapが選択されていた場合、
        // Fighterへ切り替える
        if (playerInput.currentActionMap == null ||
            playerInput.currentActionMap.name != actionMapName)
        {
            playerInput.SwitchCurrentActionMap(
                actionMapName
            );
        }
    }

    /// <summary>
    /// PlayerInputから必要なActionを取得する。
    /// </summary>
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
                $"{name}のPlayer Inputに" +
                "Input Actionsが設定されていません。",
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
                $"Action Map「{actionMapName}」が" +
                "見つかりません。",
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
    /// Action Mapから指定された名前のActionを取得する。
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
                $"Action「{actionName}」が" +
                "見つかりません。",
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
            ConvertHorizontalWithHysteresis(
                moveInput.x
            );

        int vertical =
            ConvertVerticalToDigital(
                moveInput.y
            );

        // 上方向へ入力が切り替わった瞬間だけTrue
        // 上を押し続けても連続ジャンプしない
        bool upPressedThisFrame =
            vertical == 1 &&
            previousVerticalDirection != 1;

        // SpaceまたはゲームパッドのButton South
        bool jumpButtonPressed =
            jumpAction.WasPressedThisFrame();

        FighterInputData inputData =
            new FighterInputData
            {
                horizontal = horizontal,
                vertical = vertical,

                // 上入力とジャンプボタンの両方に対応
                jumpPressed =
                    upPressedThisFrame ||
                    jumpButtonPressed,

                lightAttackPressed =
                    lightAttackAction
                        .WasPressedThisFrame(),

                heavyAttackPressed =
                    heavyAttackAction
                        .WasPressedThisFrame()
            };

        // 次のフレームで上入力の変化を比較する
        previousVerticalDirection = vertical;

        return inputData;
    }

    /// <summary>
    /// 横方向のアナログ入力を-1、0、1へ変換する。
    ///
    /// 入力開始と解除に別々のしきい値を使用し、
    /// スティックの微妙なブレでニュートラル判定が
    /// 消える問題を防ぐ。
    /// </summary>
    private int ConvertHorizontalWithHysteresis(
        float value
    )
    {
        // 現在ニュートラル
        if (currentHorizontalDirection == 0)
        {
            if (value >= directionPressThreshold)
            {
                currentHorizontalDirection = 1;
            }
            else if (value <= -directionPressThreshold)
            {
                currentHorizontalDirection = -1;
            }

            return currentHorizontalDirection;
        }

        // 現在右入力
        if (currentHorizontalDirection == 1)
        {
            if (value <= directionReleaseThreshold)
            {
                currentHorizontalDirection = 0;
            }

            return currentHorizontalDirection;
        }

        // 現在左入力
        if (value >= -directionReleaseThreshold)
        {
            currentHorizontalDirection = 0;
        }

        return currentHorizontalDirection;
    }

    /// <summary>
    /// 上下方向の入力を-1、0、1へ変換する。
    /// </summary>
    private int ConvertVerticalToDigital(
        float value
    )
    {
        if (value >= directionPressThreshold)
        {
            return 1;
        }

        if (value <= -directionPressThreshold)
        {
            return -1;
        }

        return 0;
    }

    private void OnDisable()
    {
        // 再度有効になったときに、
        // 前回の方向入力が残らないようにする
        currentHorizontalDirection = 0;
        previousVerticalDirection = 0;
    }

    private void OnValidate()
    {
        // 解除しきい値が入力開始しきい値以上になると、
        // スティック判定が不安定になるため制限する
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
