using UnityEngine;

/// <summary>
/// キャラクターの地上移動・ジャンプ・接地判定を管理する。
/// 空中では、ジャンプ開始時の横方向を維持する。
/// </summary>
public sealed class FighterMotor : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private Rigidbody2D rigidBody2D;

    [SerializeField]
    private Transform groundCheck;

    [Header("地上移動")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    [Header("ジャンプ")]
    [SerializeField, Min(0f)]
    private float jumpPower = 12f;

    [Tooltip("前・後ろジャンプ時の横移動速度")]
    [SerializeField, Min(0f)]
    private float jumpHorizontalSpeed = 4f;

    [Header("接地判定")]
    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField, Min(0.01f)]
    private float groundCheckRadius = 0.2f;

    // ジャンプ開始時に決定した横速度
    private float lockedAirVelocityX;

    // 空中の横移動が固定されているか
    private bool isAirMovementLocked;

    // 前回の接地状態
    private bool wasGrounded;

    /// <summary>
    /// 現在地面に接触しているか。
    /// </summary>
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

    private void Reset()
    {
        rigidBody2D = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rigidBody2D == null)
        {
            rigidBody2D = GetComponent<Rigidbody2D>();
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
                $"{name}のGround Checkが設定されていません。",
                this
            );
        }

        wasGrounded = IsGrounded;
    }

    /// <summary>
    /// 1回分の移動処理を実行する。
    /// FighterControllerのFixedUpdateから呼び出す。
    /// </summary>
    public void SimulateInput(
        FighterInputData input,
        bool canMove
    )
    {
        if (rigidBody2D == null)
        {
            return;
        }

        bool groundedNow = IsGrounded;

        Vector2 velocity =
            rigidBody2D.linearVelocity;

        int horizontal =
            Mathf.Clamp(input.horizontal, -1, 1);

        // 地面から落ちた場合も、
        // 落下開始時の横速度を空中で維持する
        if (wasGrounded &&
            !groundedNow &&
            !isAirMovementLocked)
        {
            lockedAirVelocityX = velocity.x;
            isAirMovementLocked = true;
        }

        // 着地したら空中移動の固定を解除する
        // 上昇開始直後に接地判定が残る場合があるため、
        // Y速度が下向きかほぼ停止中のときだけ着地と判定する
        if (groundedNow &&
            velocity.y <= 0.05f)
        {
            isAirMovementLocked = false;
            lockedAirVelocityX = 0f;
        }

        bool canJump =
            canMove &&
            input.jumpPressed &&
            groundedNow;

        if (canJump)
        {
            StartJump(
                horizontal,
                ref velocity
            );
        }
        else if (isAirMovementLocked || !groundedNow)
        {
            // 空中では左右入力を無視して、
            // ジャンプ開始時の横速度を維持する
            velocity.x = lockedAirVelocityX;
        }
        else
        {
            // 地上だけ左右入力を反映する
            velocity.x = canMove
                ? horizontal * moveSpeed
                : 0f;
        }

        rigidBody2D.linearVelocity = velocity;

        wasGrounded = groundedNow;
    }

    /// <summary>
    /// ジャンプ開始時に縦速度と横速度を決定する。
    /// </summary>
    private void StartJump(
        int horizontal,
        ref Vector2 velocity
    )
    {
        // ジャンプした瞬間の入力方向を保存する
        lockedAirVelocityX =
            horizontal * jumpHorizontalSpeed;

        isAirMovementLocked = true;

        velocity.x = lockedAirVelocityX;
        velocity.y = jumpPower;
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
