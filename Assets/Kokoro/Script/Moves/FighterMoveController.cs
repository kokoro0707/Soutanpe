using UnityEngine;

/// <summary>
/// 通常技とアシストコンボの開始、
/// フレーム進行、攻撃判定を管理する。
/// </summary>
public sealed class FighterMoveController : MonoBehaviour
{
    [Header("使用する技")]
    [SerializeField]
    private FighterMoveSet moveSet;

    [Header("参照")]
    [SerializeField]
    private AttackHitbox attackHitbox;

    [SerializeField]
    private FighterHealth ownerHealth;

    [SerializeField]
    private FighterStateMachine stateMachine;

    private MoveData currentMove;
    private int currentMoveFrame;

    // 攻撃開始時の向き
    private int attackFacingDirection = 1;

    // -1ならアシストコンボを使用していない
    private int assistComboIndex = -1;

    // 次のコンボ段が入力予約されているか
    private bool assistNextQueued;

    public bool IsAttacking =>
        currentMove != null;

    public MoveData CurrentMove =>
        currentMove;

    public int CurrentMoveFrame =>
        currentMoveFrame;

    public bool IsAssistComboActive =>
        assistComboIndex >= 0;

    private void Reset()
    {
        ownerHealth =
            GetComponent<FighterHealth>();

        stateMachine =
            GetComponent<FighterStateMachine>();

        attackHitbox =
            GetComponentInChildren<AttackHitbox>(true);
    }

    private void Awake()
    {
        if (ownerHealth == null)
        {
            ownerHealth =
                GetComponent<FighterHealth>();
        }

        if (stateMachine == null)
        {
            stateMachine =
                GetComponent<FighterStateMachine>();
        }

        if (attackHitbox == null)
        {
            attackHitbox =
                GetComponentInChildren<AttackHitbox>(true);
        }

        if (attackHitbox == null)
        {
            Debug.LogError(
                $"{name}にAttackHitboxがありません。",
                this
            );

            return;
        }

        attackHitbox.Deactivate();
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
        else
        {
            ReadAssistComboInput(command);
        }

        if (currentMove == null)
        {
            return;
        }

        UpdateCurrentMove();
    }

    /// <summary>
    /// 新しい攻撃を開始する。
    /// </summary>
    private void TryStartAttack(
        FighterCommandData command,
        int facingDirection,
        bool isGrounded
    )
    {
        if (!isGrounded)
        {
            return;
        }

        if (stateMachine == null ||
            !stateMachine.CanStartAttack)
        {
            return;
        }

        if (moveSet == null)
        {
            Debug.LogWarning(
                $"{name}にMove Setが設定されていません。",
                this
            );

            return;
        }

        // 弱攻撃ボタンをアシストコンボボタンとして使用
        if (command.lightAttackPressed)
        {
            if (moveSet.AssistCombo != null &&
                moveSet.AssistCombo.IsValid)
            {
                StartAssistCombo(
                    facingDirection
                );
            }
            else
            {
                StartNormalMove(
                    moveSet.LightAttack,
                    facingDirection
                );
            }

            return;
        }

        if (command.heavyAttackPressed)
        {
            StartNormalMove(
                moveSet.HeavyAttack,
                facingDirection
            );
        }
    }

    /// <summary>
    /// コンボ中の追加入力を確認する。
    /// </summary>
    private void ReadAssistComboInput(
        FighterCommandData command
    )
    {
        if (!IsAssistComboActive ||
            moveSet == null ||
            moveSet.AssistCombo == null)
        {
            return;
        }

        AssistComboData combo =
            moveSet.AssistCombo;

        if (assistComboIndex >=
            combo.StepCount - 1)
        {
            return;
        }

        // 連打式の場合は、同じ弱攻撃ボタンで次の段を予約
        if (combo.AdvanceMode ==
                AssistComboAdvanceMode.PressEachStep &&
            command.lightAttackPressed)
        {
            assistNextQueued = true;

            Debug.Log(
                $"{name}：アシストコンボ" +
                $"{assistComboIndex + 2}段目を予約",
                this
            );
        }
    }

    /// <summary>
    /// アシストコンボの1段目を開始する。
    /// </summary>
    private void StartAssistCombo(
        int facingDirection
    )
    {
        AssistComboData combo =
            moveSet.AssistCombo;

        AssistComboStep firstStep =
            combo.GetStep(0);

        if (firstStep == null ||
            firstStep.Move == null)
        {
            Debug.LogWarning(
                $"{name}のアシストコンボ1段目が未設定です。",
                this
            );

            return;
        }

        assistComboIndex = 0;

        // 自動式なら次の段を自動予約
        assistNextQueued =
            combo.AdvanceMode ==
                AssistComboAdvanceMode.OnePressAuto &&
            combo.StepCount > 1;

        StartMoveInternal(
            firstStep.Move,
            facingDirection
        );

        Debug.Log(
            $"{name}：アシストコンボ開始",
            this
        );
    }

    /// <summary>
    /// 通常技を開始する。
    /// </summary>
    private void StartNormalMove(
        MoveData move,
        int facingDirection
    )
    {
        ResetAssistCombo();

        StartMoveInternal(
            move,
            facingDirection
        );
    }

    /// <summary>
    /// 技を実際に開始する共通処理。
    /// </summary>
    private void StartMoveInternal(
        MoveData move,
        int facingDirection
    )
    {
        if (move == null)
        {
            Debug.LogWarning(
                $"{name}が出そうとしたMoveDataが未設定です。",
                this
            );

            ResetAssistCombo();
            return;
        }

        currentMove = move;
        currentMoveFrame = 0;

        attackFacingDirection =
            facingDirection >= 0 ? 1 : -1;

        if (stateMachine != null)
        {
            stateMachine.TryChangeState(
                FighterState.Attack
            );
        }

        if (attackHitbox != null)
        {
            attackHitbox.Deactivate();
        }

        Debug.Log(
            $"{name}：{move.MoveName}開始 " +
            $"方向：{attackFacingDirection}",
            this
        );
    }

    /// <summary>
    /// 現在の技を1フレーム進める。
    /// </summary>
    private void UpdateCurrentMove()
    {
        if (currentMove == null ||
            attackHitbox == null)
        {
            return;
        }

        UpdateHitbox();

        // 次のコンボ段へ移行できるか確認
        if (TryAdvanceAssistCombo())
        {
            return;
        }

        currentMoveFrame++;

        if (currentMoveFrame >=
            currentMove.TotalFrames)
        {
            EndMove();
        }
    }

    /// <summary>
    /// 攻撃判定のON・OFFを更新する。
    /// </summary>
    private void UpdateHitbox()
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
    }

    /// <summary>
    /// 予約された次のコンボ段へ移行する。
    /// </summary>
    private bool TryAdvanceAssistCombo()
    {
        if (!IsAssistComboActive ||
            !assistNextQueued ||
            moveSet == null ||
            moveSet.AssistCombo == null)
        {
            return false;
        }

        AssistComboData combo =
            moveSet.AssistCombo;

        AssistComboStep currentStep =
            combo.GetStep(assistComboIndex);

        if (currentStep == null ||
            !currentStep.IsCancelWindow(
                currentMoveFrame))
        {
            return false;
        }

        int nextIndex =
            assistComboIndex + 1;

        AssistComboStep nextStep =
            combo.GetStep(nextIndex);

        if (nextStep == null ||
            nextStep.Move == null)
        {
            Debug.LogWarning(
                $"{name}のアシストコンボ" +
                $"{nextIndex + 1}段目が未設定です。",
                this
            );

            assistNextQueued = false;
            return false;
        }

        assistComboIndex = nextIndex;

        // 自動式なら、さらに次の段も予約する
        assistNextQueued =
            combo.AdvanceMode ==
                AssistComboAdvanceMode.OnePressAuto &&
            assistComboIndex <
                combo.StepCount - 1;

        StartMoveInternal(
            nextStep.Move,
            attackFacingDirection
        );

        Debug.Log(
            $"{name}：アシストコンボ" +
            $"{assistComboIndex + 1}段目",
            this
        );

        return true;
    }

    /// <summary>
    /// 現在の技を終了する。
    /// </summary>
    private void EndMove()
    {
        if (attackHitbox != null)
        {
            attackHitbox.Deactivate();
        }

        if (currentMove != null)
        {
            Debug.Log(
                $"{name}：{currentMove.MoveName}終了",
                this
            );
        }

        currentMove = null;
        currentMoveFrame = 0;

        ResetAssistCombo();

        if (stateMachine != null &&
            stateMachine.CurrentState != FighterState.KO)
        {
            stateMachine.TryChangeState(
                FighterState.Idle
            );
        }
    }

    /// <summary>
    /// 被弾などで攻撃を中断する。
    /// </summary>
    public void CancelCurrentMove()
    {
        if (attackHitbox != null)
        {
            attackHitbox.Deactivate();
        }

        currentMove = null;
        currentMoveFrame = 0;

        ResetAssistCombo();
    }

    private void ResetAssistCombo()
    {
        assistComboIndex = -1;
        assistNextQueued = false;
    }

    public void SetMoveSet(
        FighterMoveSet newMoveSet
    )
    {
        CancelCurrentMove();
        moveSet = newMoveSet;
    }
}
