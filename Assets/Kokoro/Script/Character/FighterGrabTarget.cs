using UnityEngine;

/// <summary>
/// Fighterがつかまれた時の状態を管理する。
/// </summary>
public sealed class FighterGrabTarget : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private FighterStateMachine stateMachine;

    [SerializeField]
    private FighterMotor motor;

    [SerializeField]
    private FighterMoveController moveController;

    [SerializeField]
    private FighterGrabController grabController;

    [SerializeField]
    private FighterHitReceiver hitReceiver;

    [SerializeField]
    private Rigidbody2D rigidBody2D;

    public bool IsGrabbed =>
        stateMachine != null &&
        stateMachine.CurrentState ==
        FighterState.Grabbed;

    private void Reset()
    {
        stateMachine =
            GetComponent<FighterStateMachine>();

        motor =
            GetComponent<FighterMotor>();

        moveController =
            GetComponent<FighterMoveController>();

        grabController =
            GetComponent<FighterGrabController>();

        hitReceiver =
            GetComponent<FighterHitReceiver>();

        rigidBody2D =
            GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (stateMachine == null)
            stateMachine = GetComponent<FighterStateMachine>();

        if (motor == null)
            motor = GetComponent<FighterMotor>();

        if (moveController == null)
            moveController = GetComponent<FighterMoveController>();

        if (grabController == null)
            grabController = GetComponent<FighterGrabController>();

        if (hitReceiver == null)
            hitReceiver = GetComponent<FighterHitReceiver>();

        if (rigidBody2D == null)
            rigidBody2D = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// 現在つかめる状態か。
    /// </summary>
    public bool CanBeGrabbed()
    {
        if (stateMachine == null ||
            motor == null)
        {
            return false;
        }

        // 空中はつかめない
        if (!motor.IsGrounded)
        {
            return false;
        }

        FighterState state =
            stateMachine.CurrentState;

        // 最初はこの3状態だけつかめる
        return
            state == FighterState.Idle ||
            state == FighterState.Walk ||
            state == FighterState.Guard;
    }

    public bool TryBeginGrab()
    {
        if (!CanBeGrabbed())
        {
            return false;
        }

        moveController?.CancelCurrentMove();
        grabController?.CancelGrab();
        motor?.CancelSpecialMovement();

        if (rigidBody2D != null)
        {
            rigidBody2D.linearVelocity =
                Vector2.zero;
        }

        stateMachine.ForceChangeState(
            FighterState.Grabbed
        );

        return true;
    }

    /// <summary>
    /// つかみ中、相手を攻撃者の前へ固定する。
    /// </summary>
    public void SetGrabPosition(
        Vector2 worldPosition
    )
    {
        if (!IsGrabbed ||
            rigidBody2D == null)
        {
            return;
        }

        rigidBody2D.linearVelocity =
            Vector2.zero;

        rigidBody2D.position =
            worldPosition;
    }

    public void Throw(
        GrabData grabData,
        int direction
    )
    {
        if (grabData == null ||
            hitReceiver == null)
        {
            return;
        }

        hitReceiver.ReceiveThrow(
            grabData.ThrowDamage,
            grabData.ThrowKnockback,
            direction,
            grabData.ThrowHitStunFrames
        );
    }

    public void CancelBeingGrabbed()
    {
        if (!IsGrabbed)
        {
            return;
        }

        stateMachine.ForceChangeState(
            FighterState.Idle
        );
    }
}
