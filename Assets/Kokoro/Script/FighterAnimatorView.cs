using UnityEngine;

/// <summary>
/// Fighterのゲーム状態をAnimatorへ反映する。
/// 見た目だけを担当する。
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
    private Rigidbody2D rigidBody2D;

    [Header("歩行判定")]
    [SerializeField]
    private float movementThreshold = 0.05f;

    private static readonly int MoveDirectionHash =
        Animator.StringToHash("MoveDirection");

    private static readonly int IsDashingHash =
        Animator.StringToHash("IsDashing");

    private static readonly int IsForwardStepHash =
        Animator.StringToHash("IsForwardStep");

    private static readonly int IsBackStepHash =
       Animator.StringToHash("IsBackStep");

    private static readonly int IsJumpingHash =
    Animator.StringToHash("IsJumping");

    private static readonly int IsBlockingHash =
    Animator.StringToHash("IsBlocking");

    private static readonly int IsHitHash =
    Animator.StringToHash("IsHit");


    private void Reset()
    {
        animator =
            GetComponent<Animator>();

        stateMachine =
            GetComponentInParent<FighterStateMachine>();

        facingController =
            GetComponentInParent<FighterFacingController>();

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
            rigidBody2D == null)
        {
            return;
        }

        UpdateMovementAnimation();
        UpdateDashAnimation();
        UpdateForwardStepAnimation();
        UpdateBackStepAnimation();
        UpdateJumpAnimation();
        UpdateBlockAnimation();
        UpdateHitAnimation();
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

    /// <summary>
    /// ダッシュ状態をAnimatorへ送る。
    /// </summary>
    private void UpdateDashAnimation()
    {
        bool isDashing =
            stateMachine.CurrentState ==
            FighterState.Dash;

        animator.SetBool(
            IsDashingHash,
            isDashing
        );
    }
    private void UpdateForwardStepAnimation()
    {
        bool isForwardStep =
            stateMachine.CurrentState ==
            FighterState.ForwardStep;

        animator.SetBool(
            IsForwardStepHash,
            isForwardStep
        );
    }

    /// <summary>
    /// バックステップ状態をAnimatorへ送る。
    /// </summary>
    private void UpdateBackStepAnimation()
    {
        bool isBackStep =
            stateMachine.CurrentState ==
            FighterState.BackStep;

        animator.SetBool(
            IsBackStepHash,
            isBackStep
        );
    }

    /// <summary>
    /// ジャンプ状態をAnimatorへ送る。
    /// </summary>
    private void UpdateJumpAnimation()
    {
        bool isJumping =
            stateMachine.CurrentState ==
            FighterState.Jump;

        animator.SetBool(
            IsJumpingHash,
            isJumping
        );
    }

    /// <summary>
    /// 攻撃をガードした時の硬直状態をAnimatorへ送る。
    /// </summary>
    private void UpdateBlockAnimation()
    {
        bool isBlocking =
            stateMachine.CurrentState ==
            FighterState.BlockStun;

        animator.SetBool(
            IsBlockingHash,
            isBlocking
        );
    }

    /// <summary>
    /// 被弾硬直状態をAnimatorへ送る。
    /// </summary>
    private void UpdateHitAnimation()
    {
        bool isHit =
            stateMachine.CurrentState ==
            FighterState.HitStun;

        animator.SetBool(
            IsHitHash,
            isHit
        );
    }



}