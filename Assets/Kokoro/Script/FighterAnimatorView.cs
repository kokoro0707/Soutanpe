using UnityEngine;

/// <summary>
/// Fighterのゲーム状態をAnimatorへ反映する。
/// このクラスは見た目だけを担当する。
/// </summary>
public sealed class FighterAnimatorView : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private FighterStateMachine stateMachine;

    [SerializeField]
    private FighterFacingController facingController;

    [SerializeField]
    private FighterMoveController moveController;

    [SerializeField]
    private Rigidbody2D rigidBody2D;

    [SerializeField]
    private FighterMotor motor;

    [Header("歩行判定")]
    [SerializeField]
    private float movementThreshold = 0.05f;

    // =========================
    // Animator Parameters
    // =========================

    private static readonly int MoveDirectionHash =
        Animator.StringToHash("MoveDirection");

    private static readonly int AttackIndexHash =
        Animator.StringToHash("AttackIndex");

    private static readonly int ActionStateHash =
        Animator.StringToHash("ActionState");

    private void Reset()
    {
        animator =
            GetComponent<Animator>();

        stateMachine =
            GetComponentInParent<FighterStateMachine>();

        facingController =
            GetComponentInParent<FighterFacingController>();

        moveController =
            GetComponentInParent<FighterMoveController>();

        motor=
            GetComponentInParent<FighterMotor>();

        rigidBody2D =
            GetComponentInParent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponent<Animator>();
        }

        if (stateMachine == null)
        {
            stateMachine =
                GetComponentInParent<FighterStateMachine>();
        }

        if (facingController == null)
        {
            facingController =
                GetComponentInParent<FighterFacingController>();
        }

        if (moveController == null)
        {
            moveController =
                GetComponentInParent<FighterMoveController>();
        }

        if(motor==null)
        {
            motor=
                GetComponentInParent<FighterMotor>();
        }

        if (rigidBody2D == null)
        {
            rigidBody2D =
                GetComponentInParent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        if (animator == null ||
            stateMachine == null ||
            facingController == null ||
            rigidBody2D == null||            
            motor==null)
        {
            return;
        }

        UpdateMovementAnimation();
        UpdateActionAnimation();
        UpdateAttackAnimation();
    }

    /// <summary>
    /// 前歩き・後ろ歩きをAnimatorへ送る。
    /// </summary>
    private void UpdateMovementAnimation()
    {
        int moveDirection = 0;

        FighterState currentState =
            stateMachine.CurrentState;

        bool canUseWalkAnimation =
            currentState == FighterState.Walk ||
            currentState == FighterState.Guard;

        if (canUseWalkAnimation)
        {
            float relativeVelocity =
                rigidBody2D.linearVelocity.x *
                facingController.FacingDirection;

            if (relativeVelocity >
                movementThreshold)
            {
                // 前歩き
                moveDirection = 1;
            }
            else if (relativeVelocity <
                     -movementThreshold)
            {
                // 後ろ歩き
                moveDirection = -1;
            }
        }

        animator.SetInteger(
            MoveDirectionHash,
            moveDirection
        );
    }

    private void UpdateActionAnimation()
    {
        int actionState = 0;

        // =========================
        // 戦闘状態を優先
        // =========================

        switch (stateMachine.CurrentState)
        {
            case FighterState.BlockStun:
                actionState = 5;
                break;

            case FighterState.HitStun:
                actionState = 6;
                break;

            case FighterState.Grab:
                actionState = 7;
                break;

            case FighterState.Throw:
                actionState = 8;
                break;

            case FighterState.Attack:
                // 攻撃はAttackIndex側で管理する
                actionState = 0;
                break;

            default:

                // =========================
                // 実際の移動状態を見る
                // =========================

                switch (motor.CurrentMode)
                {
                    case FighterLocomotionMode.Dash:
                        actionState = 1;
                        break;

                    case FighterLocomotionMode.ForwardStep:
                        actionState = 2;
                        break;

                    case FighterLocomotionMode.BackStep:
                        actionState = 3;
                        break;

                    case FighterLocomotionMode.Air:
                        actionState = 4;
                        break;

                    default:
                        actionState = 0;
                        break;
                }

                break;
        }

        animator.SetInteger(
            ActionStateHash,
            actionState
        );
    }


    /// <summary>
    /// 現在実行しているMoveDataの
    /// AnimationIndexをAnimatorへ送る。
    /// </summary>
    private void UpdateAttackAnimation()
    {
        int attackIndex = 0;

        if (stateMachine.CurrentState ==
                FighterState.Attack &&
            moveController != null &&
            moveController.CurrentMove != null)
        {
            attackIndex =
                moveController.CurrentMove.AnimationIndex;
        }

        animator.SetInteger(
            AttackIndexHash,
            attackIndex
        );
    }
}
