using UnityEngine;

/// <summary>
/// 技の開始、フレーム進行、攻撃判定のON・OFFを管理する。
/// アニメーションがなくても動作する。
/// </summary>
public sealed class FighterMoveController :
    MonoBehaviour
{
    [Header("技データ")]
    [SerializeField]
    private MoveData lightAttack;

    [Header("参照")]
    [SerializeField]
    private AttackHitbox attackHitbox;

    [SerializeField]
    private FighterHealth ownerHealth;

    [SerializeField]
    private FighterStateMachine stateMachine;

    private MoveData currentMove;
    private int currentMoveFrame;
    private int attackFacingDirection;

    public bool IsAttacking =>
        currentMove != null;

    public MoveData CurrentMove =>
        currentMove;

    public int CurrentMoveFrame =>
        currentMoveFrame;

    private void Reset()
    {
        ownerHealth =
            GetComponent<FighterHealth>();

        stateMachine =
            GetComponent<FighterStateMachine>();

        attackHitbox =
            GetComponentInChildren<AttackHitbox>();
    }

    private void Awake()
    {
        if (attackHitbox != null)
        {
            attackHitbox.Deactivate();
        }
    }

    /// <summary>
    /// 1フレーム分の攻撃処理を進める。
    /// </summary>
    public void SimulateCommand(
        FighterCommandData command,
        int facingDirection,
        bool isGrounded
    )
    {
        if (currentMove == null)
        {
            TryStartAttack(
                command,
                facingDirection,
                isGrounded
            );
        }

        if (currentMove == null)
        {
            return;
        }

        UpdateCurrentMove();
    }

    private void TryStartAttack(
        FighterCommandData command,
        int facingDirection,
        bool isGrounded
    )
    {
        if (!command.lightAttackPressed)
        {
            return;
        }

        if (!isGrounded)
        {
            return;
        }

        if (stateMachine == null ||
            !stateMachine.CanStartAttack)
        {
            return;
        }

        StartMove(
            lightAttack,
            facingDirection
        );
    }

    private void StartMove(
        MoveData move,
        int facingDirection
    )
    {
        if (move == null)
        {
            Debug.LogWarning(
                $"{name}のLight AttackにMoveDataが設定されていません。",
                this
            );

            return;
        }

        currentMove = move;
        currentMoveFrame = 0;

        attackFacingDirection =
            facingDirection >= 0 ? 1 : -1;

        stateMachine.TryChangeState(
            FighterState.Attack
        );

        attackHitbox.Deactivate();

        Debug.Log(
            $"{name}：{move.MoveName}開始",
            this
        );
    }

    private void UpdateCurrentMove()
    {
        bool shouldEnableHitbox =
            currentMove.IsActiveFrame(
                currentMoveFrame
            );

        if (shouldEnableHitbox)
        {
            if (!attackHitbox.IsActive)
            {
                attackHitbox.Activate(
                    currentMove,
                    attackFacingDirection,
                    ownerHealth
                );
            }
        }
        else if (attackHitbox.IsActive)
        {
            attackHitbox.Deactivate();
        }

        currentMoveFrame++;

        if (currentMoveFrame >=
            currentMove.TotalFrames)
        {
            EndMove();
        }
    }

    private void EndMove()
    {
        attackHitbox.Deactivate();

        Debug.Log(
            $"{name}：{currentMove.MoveName}終了",
            this
        );

        currentMove = null;
        currentMoveFrame = 0;

        stateMachine.TryChangeState(
            FighterState.Idle
        );
    }

    public void CancelCurrentMove()
    {
        if (currentMove == null)
        {
            return;
        }

        attackHitbox.Deactivate();

        currentMove = null;
        currentMoveFrame = 0;
    }
}
