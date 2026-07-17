using UnityEngine;

public enum FighterLocomotionMode
{
    Ground,
    Air,
    ForwardStep,
    BackStep,
    Dash
}

/// <summary>
/// キャラクターの移動処理を担当する。
/// 入力取得や2回入力判定は行わない。
/// </summary>
public sealed class FighterMotor : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private Rigidbody2D rigidBody2D;

    [SerializeField]
    private Transform groundCheck;

    [Header("歩行")]
    [SerializeField, Min(0f)]
    private float forwardWalkSpeed = 5f;
    [SerializeField, Min(0f)]
    private float backwardWalkSpeed = 3.5f;

    [Header("ジャンプ")]
    [SerializeField, Min(0f)]
    private float jumpPower = 12f;

    [SerializeField, Min(0f)]
    private float jumpHorizontalSpeed = 4f;

    [Header("前ステップ")]
    [SerializeField, Min(0f)]
    private float forwardStepSpeed = 9f;

    [SerializeField, Min(1)]
    private int forwardStepFrames = 10;

    [Header("バックステップ")]
    [SerializeField, Min(0f)]
    private float backStepSpeed = 8f;

    [SerializeField, Min(1)]
    private int backStepFrames = 12;

    [Header("ダッシュ")]
    [SerializeField, Min(0f)]
    private float dashSpeed = 7f;

    [Header("接地判定")]
    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField, Min(0.01f)]
    private float groundCheckRadius = 0.2f;

    private float lockedAirVelocityX;
    private int movementFrame;

    public FighterLocomotionMode CurrentMode
    {
        get;
        private set;
    } = FighterLocomotionMode.Ground;

    public bool IsGrounded
    {
        get
        {
            if (groundCheck == null)
            {
                return false;
            }

            return Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            ) != null;
        }
    }

    public bool CanAutoTurn =>
        CurrentMode == FighterLocomotionMode.Ground;

    private void Reset()
    {
        rigidBody2D = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rigidBody2D == null)
        {
            rigidBody2D =
                GetComponent<Rigidbody2D>();
        }

        if (rigidBody2D == null)
        {
            Debug.LogError(
                $"{name}にRigidbody2Dがありません。",
                this
            );
        }

        if (groundCheck == null)
        {
            Debug.LogError(
                $"{name}のGround Checkが未設定です。",
                this
            );
        }
    }

    public void SimulateCommand(
        FighterCommandData command,
        bool canStartMovement,
        int facingDirection
    )
    {
        if (rigidBody2D == null)
        {
            return;
        }

        facingDirection =
            facingDirection >= 0 ? 1 : -1;

        Vector2 velocity =
            rigidBody2D.linearVelocity;

        bool groundedNow = IsGrounded;

        // ステージ端などから落下した場合
        if (!groundedNow &&
            CurrentMode != FighterLocomotionMode.Air)
        {
            lockedAirVelocityX = velocity.x;
            CurrentMode = FighterLocomotionMode.Air;
        }

        // 着地判定
        if (CurrentMode ==
                FighterLocomotionMode.Air &&
            groundedNow &&
            velocity.y <= 0.05f)
        {
            CurrentMode =
                FighterLocomotionMode.Ground;

            lockedAirVelocityX = 0f;
        }

        switch (CurrentMode)
        {
            case FighterLocomotionMode.Ground:
                UpdateGroundMovement(
                    command,
                    canStartMovement,
                    facingDirection,
                    ref velocity
                );
                break;

            case FighterLocomotionMode.Air:
                UpdateAirMovement(
                    ref velocity
                );
                break;

            case FighterLocomotionMode.ForwardStep:
                UpdateForwardStep(
                    command,
                    facingDirection,
                    ref velocity
                );
                break;

            case FighterLocomotionMode.BackStep:
                UpdateBackStep(
                    facingDirection,
                    ref velocity
                );
                break;

            case FighterLocomotionMode.Dash:
                UpdateDash(
                    command,
                    facingDirection,
                    ref velocity
                );
                break;
        }

        rigidBody2D.linearVelocity = velocity;
    }

    private void UpdateGroundMovement(
    FighterCommandData command,
    bool canStartMovement,
    int facingDirection,
    ref Vector2 velocity
)
    {
        if (!canStartMovement)
        {
            velocity.x = 0f;
            return;
        }

        // ジャンプを優先
        if (command.jumpPressed)
        {
            StartJump(
                command.horizontal,
                ref velocity
            );

            return;
        }

        // 後ろ2回入力
        if (command.backStepPressed)
        {
            StartBackStep(
                facingDirection,
                ref velocity
            );

            return;
        }

        // 前2回入力
        if (command.forwardStepPressed)
        {
            StartForwardStep(
                facingDirection,
                ref velocity
            );

            return;
        }

        // キャラクターの向きを基準に、
        // 前入力か後ろ入力かを判定する
        int relativeDirection =
            command.horizontal * facingDirection;

        if (relativeDirection < 0)
        {
            // 後ろへ歩きながらガード可能
            velocity.x =
                command.horizontal * backwardWalkSpeed;

            return;
        }

        if (relativeDirection > 0)
        {
            // 前歩き
            velocity.x =
                command.horizontal * forwardWalkSpeed;

            return;
        }

        // 横入力なし
        velocity.x = 0f;
    }


    private void StartJump(
        int horizontal,
        ref Vector2 velocity
    )
    {
        lockedAirVelocityX =
            Mathf.Clamp(horizontal, -1, 1) *
            jumpHorizontalSpeed;

        velocity.x = lockedAirVelocityX;
        velocity.y = jumpPower;

        CurrentMode =
            FighterLocomotionMode.Air;
    }

    private void UpdateAirMovement(
        ref Vector2 velocity
    )
    {
        // ジャンプ開始時の方向を着地まで維持する
        velocity.x = lockedAirVelocityX;
    }

    private void StartForwardStep(
        int facingDirection,
        ref Vector2 velocity
    )
    {
        movementFrame = 0;

        CurrentMode =
            FighterLocomotionMode.ForwardStep;

        velocity.x =
            facingDirection * forwardStepSpeed;
    }

    private void UpdateForwardStep(
        FighterCommandData command,
        int facingDirection,
        ref Vector2 velocity
    )
    {
        movementFrame++;

        velocity.x =
            facingDirection * forwardStepSpeed;

        // 2回目を長押ししていたらダッシュへ移行
        if (command.dashHeld)
        {
            CurrentMode =
                FighterLocomotionMode.Dash;

            velocity.x =
                facingDirection * dashSpeed;

            return;
        }

        if (movementFrame >= forwardStepFrames)
        {
            EndSpecialMovement(
                ref velocity
            );
        }
    }

    private void StartBackStep(
        int facingDirection,
        ref Vector2 velocity
    )
    {
        movementFrame = 0;

        CurrentMode =
            FighterLocomotionMode.BackStep;

        velocity.x =
            -facingDirection * backStepSpeed;
    }

    private void UpdateBackStep(
        int facingDirection,
        ref Vector2 velocity
    )
    {
        movementFrame++;

        velocity.x =
            -facingDirection * backStepSpeed;

        if (movementFrame >= backStepFrames)
        {
            EndSpecialMovement(
                ref velocity
            );
        }
    }

    private void UpdateDash(
        FighterCommandData command,
        int facingDirection,
        ref Vector2 velocity
    )
    {
        bool holdingForward =
            command.horizontal *
            facingDirection > 0;

        // 2回目の前入力を離したらダッシュ終了
        if (!command.dashHeld ||
            !holdingForward)
        {
            EndSpecialMovement(
                ref velocity
            );

            return;
        }

        velocity.x =
            facingDirection * dashSpeed;
    }

    private void EndSpecialMovement(
        ref Vector2 velocity
    )
    {
        movementFrame = 0;

        CurrentMode =
            FighterLocomotionMode.Ground;

        velocity.x = 0f;
    }

    /// <summary>
    /// 被弾や攻撃開始時にステップ・ダッシュを中断する。
    /// </summary>
    public void CancelSpecialMovement()
    {
        movementFrame = 0;
        lockedAirVelocityX = 0f;

        if (rigidBody2D != null)
        {
            Vector2 velocity =
                rigidBody2D.linearVelocity;

            velocity.x = 0f;

            rigidBody2D.linearVelocity = velocity;
        }

        CurrentMode =
            IsGrounded
                ? FighterLocomotionMode.Ground
                : FighterLocomotionMode.Air;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}
