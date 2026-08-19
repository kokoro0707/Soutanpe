using UnityEngine;

public sealed class FighterController : MonoBehaviour
{
    [Header("入力")]
    [SerializeField]
    private MonoBehaviour inputSourceComponent;

    [SerializeField]
    private bool useLocalInput = true;

    [Header("キャラクター機能")]
    [SerializeField]
    private FighterCommandInterpreter commandInterpreter;

    [SerializeField]
    private FighterMotor motor;

    [SerializeField]
    private FighterMoveController moveController;

    [SerializeField]
    private FighterHitReceiver hitReceiver;

    [SerializeField]
    private FighterFacingController facingController;

    [SerializeField]
    private FighterStateMachine stateMachine;

    private IFighterInputSource inputSource;

    private FighterInputData latestInput;

    private bool jumpQueued;
    private bool lightAttackQueued;
    private bool heavyAttackQueued;
    private bool assistComboQueued;

    private int simulationFrame;

    private void Awake()
    {
        inputSource =
            inputSourceComponent as IFighterInputSource;

        if (useLocalInput &&
            inputSource == null)
        {
            Debug.LogError(
                $"{name}のInput Source Componentが不正です。",
                this
            );
        }

        if (hitReceiver == null)
        {
            hitReceiver =
                GetComponent<FighterHitReceiver>();
        }
    }

    private void Update()
    {
        if (!useLocalInput ||
            inputSource == null)
        {
            return;
        }

        FighterInputData input =
            inputSource.ReadInput();

        SetInput(input);
    }

    private void FixedUpdate()
    {
        bool canTurn =
            motor.CanAutoTurn &&
            stateMachine.CanAutoTurn &&
            motor.IsGrounded;

        facingController.RefreshFacing(
            canTurn
        );

        FighterInputData simulationInput =
            CreateSimulationInput();

        FighterCommandData command =
            commandInterpreter.BuildCommand(
                simulationInput,
                simulationFrame,
                facingController.FacingDirection
            );

        if (hitReceiver != null)
        {
            hitReceiver.SetGuardInput(
                command.guardHeld
            );
        }

        if (stateMachine.CurrentState ==
            FighterState.KO)
        {
            FinishSimulationFrame();
            return;
        }

        if (hitReceiver != null)
        {
            hitReceiver.SimulateFrame();

            if (hitReceiver.IsInReaction)
            {
                FinishSimulationFrame();
                return;
            }
        }

        moveController.SimulateCommand(
            command,
            facingController.FacingDirection,
            motor.IsGrounded
        );

        motor.SimulateCommand(
            command,
            stateMachine.CanStartMovement,
            facingController.FacingDirection
        );

        UpdateMovementState(command);

        FinishSimulationFrame();
    }

    public void SetInput(
        FighterInputData input
    )
    {
        latestInput.horizontal =
            input.horizontal;

        latestInput.vertical =
            input.vertical;

        if (input.jumpPressed)
            jumpQueued = true;

        if (input.lightAttackPressed)
            lightAttackQueued = true;

        if (input.heavyAttackPressed)
            heavyAttackQueued = true;

        if (input.assistComboPressed)
            assistComboQueued = true;
    }

    private FighterInputData
        CreateSimulationInput()
    {
        FighterInputData input =
            latestInput;

        input.jumpPressed =
            jumpQueued;

        input.lightAttackPressed =
            lightAttackQueued;

        input.heavyAttackPressed =
            heavyAttackQueued;

        input.assistComboPressed =
            assistComboQueued;

        return input;
    }

    private void FinishSimulationFrame()
    {
        jumpQueued = false;
        lightAttackQueued = false;
        heavyAttackQueued = false;
        assistComboQueued = false;

        simulationFrame++;
    }

    private void UpdateMovementState(
        FighterCommandData command
    )
    {
        if (stateMachine.IsCombatLocked)
            return;

        switch (motor.CurrentMode)
        {
            case FighterLocomotionMode.Air:

                stateMachine.TryChangeState(
                    FighterState.Jump
                );

                return;

            case FighterLocomotionMode.ForwardStep:

                stateMachine.TryChangeState(
                    FighterState.ForwardStep
                );

                return;

            case FighterLocomotionMode.BackStep:

                stateMachine.TryChangeState(
                    FighterState.BackStep
                );

                return;

            case FighterLocomotionMode.Dash:

                stateMachine.TryChangeState(
                    FighterState.Dash
                );

                return;
        }

        if (command.guardHeld)
        {
            stateMachine.TryChangeState(
                FighterState.Guard
            );

            return;
        }

        if (command.horizontal != 0)
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

    public void SetUseLocalInput(
        bool value
    )
    {
        useLocalInput = value;
    }
}
