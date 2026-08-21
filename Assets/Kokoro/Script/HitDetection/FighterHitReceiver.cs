using UnityEngine;

/// <summary>
/// 攻撃を受けたときのガード判定、
/// ダメージ、硬直、ノックバックを管理する。
/// </summary>
public sealed class FighterHitReceiver : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private FighterHealth health;

    [SerializeField]
    private FighterStateMachine stateMachine;

    [SerializeField]
    private FighterMotor motor;

    [SerializeField]
    private FighterMoveController moveController;

    [SerializeField]
    private FighterFacingController facingController;

    [SerializeField]
    private Rigidbody2D rigidBody2D;

    [Header("ノックバック")]
    [Tooltip("硬直中に横速度を毎フレーム弱める倍率")]
    [SerializeField, Range(0f, 1f)]
    private float horizontalDamping = 0.88f;

    [Tooltip("これより小さい横速度は0にする")]
    [SerializeField, Min(0f)]
    private float stopVelocityThreshold = 0.05f;

    private int reactionFramesRemaining;

    // 現在、後ろ方向を入力しているか
    private bool guardInputHeld;

    public bool IsInReaction =>
        reactionFramesRemaining > 0;

    public FighterHealth OwnerHealth =>
        health;

    private void Reset()
    {
        health =
            GetComponent<FighterHealth>();

        stateMachine =
            GetComponent<FighterStateMachine>();

        motor =
            GetComponent<FighterMotor>();

        moveController =
            GetComponent<FighterMoveController>();

        facingController =
            GetComponent<FighterFacingController>();

        rigidBody2D =
            GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (health == null)
        {
            health =
                GetComponent<FighterHealth>();
        }

        if (stateMachine == null)
        {
            stateMachine =
                GetComponent<FighterStateMachine>();
        }

        if (motor == null)
        {
            motor =
                GetComponent<FighterMotor>();
        }

        if (moveController == null)
        {
            moveController =
                GetComponent<FighterMoveController>();
        }

        if (facingController == null)
        {
            facingController =
                GetComponent<FighterFacingController>();
        }

        if (rigidBody2D == null)
        {
            rigidBody2D =
                GetComponent<Rigidbody2D>();
        }
    }

    /// <summary>
    /// 現在のガード入力を受け取る。
    /// FighterControllerから毎フレーム呼ぶ。
    /// </summary>
    public void SetGuardInput(bool isHeld)
    {
        guardInputHeld = isHeld;
    }

    /// <summary>
    /// 相手の攻撃を受け取る。
    /// </summary>
    public void ReceiveAttack(
        MoveData move,
        int attackDirection,
        Transform attacker
    )
    {
        if (move == null ||
            health == null ||
            health.IsKnockedOut)
        {
            return;
        }

        attackDirection =
            attackDirection >= 0 ? 1 : -1;

        // 攻撃やステップ中なら中断する
        if (moveController != null)
        {
            moveController.CancelCurrentMove();
        }

        if (motor != null)
        {
            motor.CancelSpecialMovement();
        }

        if (CanGuardAttack(attacker))
        {
            ReceiveBlock(
                move,
                attackDirection
            );
        }
        else
        {
            ReceiveHit(
                move,
                attackDirection
            );
        }
    }

    /// <summary>
    /// ガード不能の投げダメージを受ける。
    /// </summary>
    public void ReceiveThrow(
        int damage,
        Vector2 knockback,
        int direction,
        int hitStunFrames
    )
    {
        if (health == null ||
            health.IsKnockedOut)
        {
            return;
        }

        direction =
            direction >= 0 ? 1 : -1;

        if (moveController != null)
        {
            moveController.CancelCurrentMove();
        }

        if (motor != null)
        {
            motor.CancelSpecialMovement();
        }

        health.TakeDamage(damage);

        ApplyKnockback(
            knockback,
            direction
        );

        if (health.IsKnockedOut)
        {
            reactionFramesRemaining = 0;
            return;
        }

        reactionFramesRemaining =
            Mathf.Max(
                1,
                hitStunFrames
            );

        stateMachine.ForceChangeState(
            FighterState.HitStun
        );

        Debug.Log(
            $"{name}が投げを受けました。",
            this
        );
    }


    /// <summary>
    /// 現在の攻撃をガードできるか。
    /// </summary>
    private bool CanGuardAttack(
        Transform attacker
    )
    {
        if (!guardInputHeld)
        {
            return false;
        }

        if (motor == null ||
            !motor.IsGrounded)
        {
            return false;
        }

        if (stateMachine == null)
        {
            return false;
        }

        FighterState state =
            stateMachine.CurrentState;

        // 攻撃中・被弾中・ダウン中はガードできない
        if (state == FighterState.Attack ||
            state == FighterState.HitStun ||
            state == FighterState.KnockDown ||
            state == FighterState.KO)
        {
            return false;
        }

        if (attacker == null ||
            facingController == null)
        {
            return true;
        }

        float attackerDifference =
            attacker.position.x -
            transform.position.x;

        if (Mathf.Abs(attackerDifference) <
            0.001f)
        {
            return true;
        }

        int attackerSide =
            attackerDifference > 0f ? 1 : -1;

        // 自分が向いている側から来た攻撃だけガードする。
        // 背後からの攻撃はガードできない。
        return attackerSide ==
               facingController.FacingDirection;
    }

    private void ReceiveBlock(
        MoveData move,
        int attackDirection
    )
    {
        reactionFramesRemaining =
            move.BlockStunFrames;

        stateMachine.ForceChangeState(
            FighterState.BlockStun
        );

        ApplyKnockback(
            move.BlockKnockback,
            attackDirection
        );

        Debug.Log(
            $"{name}が{move.MoveName}をガード " +
            $"ガード硬直：{reactionFramesRemaining}",
            this
        );
    }

    private void ReceiveHit(
        MoveData move,
        int attackDirection
    )
    {
        health.TakeDamage(move.Damage);

        ApplyKnockback(
            move.HitKnockback,
            attackDirection
        );

        // FighterHealth側でKO状態に変更される
        if (health.IsKnockedOut)
        {
            reactionFramesRemaining = 0;
            return;
        }

        reactionFramesRemaining =
            move.HitStunFrames;

        stateMachine.ForceChangeState(
            FighterState.HitStun
        );

        Debug.Log(
            $"{name}がヒット硬直 " +
            $"残り：{reactionFramesRemaining}",
            this
        );
    }

    private void ApplyKnockback(
        Vector2 knockback,
        int attackDirection
    )
    {
        if (rigidBody2D == null)
        {
            return;
        }

        Vector2 velocity =
            new Vector2(
                Mathf.Abs(knockback.x) *
                attackDirection,
                knockback.y
            );

        rigidBody2D.linearVelocity =
            velocity;
    }

    /// <summary>
    /// 硬直を1フレーム進める。
    /// </summary>
    public void SimulateFrame()
    {
        if (!IsInReaction)
        {
            return;
        }

        ApplyHorizontalDamping();

        reactionFramesRemaining--;

        if (reactionFramesRemaining <= 0)
        {
            EndReaction();
        }
    }

    private void ApplyHorizontalDamping()
    {
        if (rigidBody2D == null)
        {
            return;
        }

        Vector2 velocity =
            rigidBody2D.linearVelocity;

        velocity.x *= horizontalDamping;

        if (Mathf.Abs(velocity.x) <=
            stopVelocityThreshold)
        {
            velocity.x = 0f;
        }

        rigidBody2D.linearVelocity =
            velocity;
    }

    private void EndReaction()
    {
        reactionFramesRemaining = 0;

        if (rigidBody2D != null)
        {
            Vector2 velocity =
                rigidBody2D.linearVelocity;

            velocity.x = 0f;

            rigidBody2D.linearVelocity =
                velocity;
        }

        if (stateMachine == null ||
            stateMachine.CurrentState ==
                FighterState.KO)
        {
            return;
        }

        FighterState nextState =
            motor != null &&
            !motor.IsGrounded
                ? FighterState.Jump
                : FighterState.Idle;

        stateMachine.ForceChangeState(
            nextState
        );
    }
}
