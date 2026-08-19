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
}