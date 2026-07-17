using UnityEngine;

/// <summary>
/// 入力データをキャラクターの各機能へ渡す窓口。
/// 入力元がキーボード、ゲームパッド、通信のどれでも
/// 同じFighterInputDataとして処理する。
/// </summary>
public sealed class FighterController : MonoBehaviour
{
    [Header("入力")]
    [SerializeField]
    private MonoBehaviour inputSourceComponent;

    [Tooltip("オンライン入力へ切り替える場合はOFF")]
    [SerializeField]
    private bool useLocalInput = true;

    [Header("キャラクター機能")]
    [SerializeField]
    private FighterMotor motor;

    [SerializeField]
    private FighterStateMachine stateMachine;

    private IFighterInputSource inputSource;

    // 継続入力
    private FighterInputData latestInput;

    // 1回だけ発生する入力をFixedUpdateまで保持する
    private bool jumpQueued;
    private bool lightAttackQueued;
    private bool heavyAttackQueued;

    private void Awake()
    {
        inputSource =
            inputSourceComponent as IFighterInputSource;

        if (useLocalInput && inputSource == null)
        {
            Debug.LogError(
                $"{name}のInput Source Componentに、" +
                "IFighterInputSourceを実装したスクリプトを設定してください。",
                this
            );
        }
    }

    private void Update()
    {
        if (!useLocalInput || inputSource == null)
        {
            return;
        }

        FighterInputData input =
            inputSource.ReadInput();

        SetInput(input);
    }

    private void FixedUpdate()
    {
        FighterInputData simulationInput =
            CreateSimulationInput();

        motor.SimulateInput(
            simulationInput,
            stateMachine.CanMove
        );

        UpdateMovementState(simulationInput);

        // このFixedUpdateで消費した単発入力を解除する
        ClearQueuedInputs();
    }

    /// <summary>
    /// ローカル・オンライン・CPU入力の共通入口。
    /// </summary>
    public void SetInput(FighterInputData input)
    {
        // 方向とガードは押している間継続する
        latestInput.horizontal = input.horizontal;
        latestInput.vertical = input.vertical;
        latestInput.guardHeld = input.guardHeld;

        // ボタン入力はFixedUpdateまで保持する
        if (input.jumpPressed)
        {
            jumpQueued = true;
        }

        if (input.lightAttackPressed)
        {
            lightAttackQueued = true;
        }

        if (input.heavyAttackPressed)
        {
            heavyAttackQueued = true;
        }
    }

    /// <summary>
    /// 今回のゲーム処理で使う入力を作成する。
    /// </summary>
    private FighterInputData CreateSimulationInput()
    {
        FighterInputData input = latestInput;

        input.jumpPressed = jumpQueued;
        input.lightAttackPressed = lightAttackQueued;
        input.heavyAttackPressed = heavyAttackQueued;

        return input;
    }

    private void ClearQueuedInputs()
    {
        jumpQueued = false;
        lightAttackQueued = false;
        heavyAttackQueued = false;
    }

    private void UpdateMovementState(
        FighterInputData input
    )
    {
        FighterState currentState =
            stateMachine.CurrentState;

        if (currentState == FighterState.KO ||
            currentState == FighterState.Attack ||
            currentState == FighterState.HitStun ||
            currentState == FighterState.BlockStun ||
            currentState == FighterState.KnockDown)
        {
            return;
        }

        if (!motor.IsGrounded)
        {
            stateMachine.TryChangeState(
                FighterState.Jump
            );

            return;
        }

        if (input.horizontal != 0)
        {
            stateMachine.TryChangeState(
                FighterState.Walk
            );
        }
        else
        {
            stateMachine.TryChangeState(
                FighterState.Idle
            );
        }
    }

    /// <summary>
    /// オンライン側から入力を渡す場合に切り替える。
    /// </summary>
    public void SetUseLocalInput(bool value)
    {
        useLocalInput = value;
    }
}
