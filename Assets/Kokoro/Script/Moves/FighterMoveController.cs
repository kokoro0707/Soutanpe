using UnityEngine;

/// <summary>
/// 現在実行中のコンボ種類。
/// </summary>
public enum FighterComboType
{
    None,
    Light,
    Heavy,
    Assist
}

/// <summary>
/// 弱コンボ・強コンボ・アシストコンボと
/// 各MoveDataの進行を管理する。
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

    private int attackFacingDirection = 1;

    // 現在のコンボ種類
    private FighterComboType currentComboType =
        FighterComboType.None;

    // 現在何段目か
    private int currentComboIndex = -1;

    // 弱・強コンボの次段入力予約
    private bool nextNormalStepQueued;

    //1ジャンプ中に空中攻撃を使用したか
    private bool airAttackUsed;

    public bool IsAttacking =>
        currentMove != null;

    public MoveData CurrentMove =>
        currentMove;

    public int CurrentMoveFrame =>
        currentMoveFrame;

    public FighterComboType CurrentComboType =>
        currentComboType;

    public int CurrentComboIndex =>
        currentComboIndex;

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

        if (attackHitbox != null)
        {
            attackHitbox.Deactivate();
        }
    }

    /// <summary>
    /// 1フレーム分の攻撃処理。
    /// </summary>
    public void SimulateCommand(
        FighterCommandData command,
        int facingDirection,
        bool isGrounded
    )
    {
        // 着地していたら空中攻撃を再使用可能にする
        if (isGrounded)
        {
            airAttackUsed = false;
        }

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
            ReadComboInput(command);
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
        if (moveSet == null)
        {
            Debug.LogWarning(
                $"{name}にMove Setが設定されていません。",
                this
            );

            return;
        }

        // =========================
        // 空中攻撃
        // =========================

        if (!isGrounded)
        {
            // 1ジャンプにつき1回だけ
            if (airAttackUsed)
            {
                return;
            }

            // 空中で弱攻撃
            if (command.lightAttackPressed)
            {
                StartAirAttack(
                    moveSet.JumpAttack,
                    facingDirection
                );
            }

            return;
        }

        // =========================
        // ここから地上攻撃
        // =========================

        if (stateMachine == null ||
            !stateMachine.CanStartAttack)
        {
            return;
        }

        // 下 + 必殺技
        if (command.downSpecialPressed)
        {
            StartSpecialMove(
                moveSet.DownSpecial,
                facingDirection
            );

            return;
        }

        // 前 + 必殺技
        if (command.forwardSpecialPressed)
        {
            StartSpecialMove(
                moveSet.ForwardSpecial,
                facingDirection
            );

            return;
        }

        // アシストコンボ
        if (command.assistComboPressed)
        {
            StartAssistCombo(
                moveSet.AssistCombo,
                facingDirection
            );

            return;
        }

        // 強コンボ
        if (command.heavyAttackPressed)
        {
            StartNormalCombo(
                moveSet.HeavyCombo,
                FighterComboType.Heavy,
                facingDirection
            );

            return;
        }

        // 弱コンボ
        if (command.lightAttackPressed)
        {
            StartNormalCombo(
                moveSet.LightCombo,
                FighterComboType.Light,
                facingDirection
            );
        }
    }



    /// <summary>
    /// 前必殺技・下必殺技など、
    /// 単発の必殺技を開始する。
    /// </summary>
    private void StartSpecialMove(
        MoveData move,
        int facingDirection
    )
    {
        if (move == null)
        {
            Debug.LogWarning(
                $"{name}の必殺技MoveDataが設定されていません。",
                this
            );

            return;
        }

        // 通常コンボ状態を解除
        ResetCombo();

        StartMoveInternal(
            move,
            facingDirection
        );

        Debug.Log(
            $"{name}：必殺技 {move.MoveName} 開始",
            this
        );
    }


    /// <summary>
    /// 弱・強コンボを開始する。
    /// </summary>
    private void StartNormalCombo(
        NormalComboData combo,
        FighterComboType comboType,
        int facingDirection
    )
    {
        if (combo == null ||
            !combo.IsValid)
        {
            Debug.LogWarning(
                $"{name}の{comboType} Comboが未設定です。",
                this
            );

            return;
        }

        NormalComboStep firstStep =
            combo.GetStep(0);

        if (firstStep == null ||
            firstStep.Move == null)
        {
            return;
        }

        currentComboType = comboType;
        currentComboIndex = 0;

        nextNormalStepQueued = false;

        StartMoveInternal(
            firstStep.Move,
            facingDirection
        );
    }

    /// <summary>
    /// 空中弱攻撃を開始する。
    /// 1ジャンプにつき1回だけ使用可能。
    /// </summary>
    private void StartAirAttack(
        MoveData move,
        int facingDirection
    )
    {
        if (move == null)
        {
            Debug.LogWarning(
                $"{name}のJump Attackが設定されていません。",
                this
            );

            return;
        }

        // このジャンプではもう使用済みにする
        airAttackUsed = true;

        // 通常コンボとは別扱い
        ResetCombo();

        StartMoveInternal(
            move,
            facingDirection
        );

        Debug.Log(
            $"{name}：ジャンプ攻撃開始",
            this
        );
    }


    /// <summary>
    /// アシストコンボ開始。
    /// </summary>
    private void StartAssistCombo(
        AssistComboData combo,
        int facingDirection
    )
    {
        if (combo == null ||
            !combo.IsValid)
        {
            Debug.LogWarning(
                $"{name}のAssist Comboが未設定です。",
                this
            );

            return;
        }

        AssistComboStep firstStep =
            combo.GetStep(0);

        if (firstStep == null ||
            firstStep.Move == null)
        {
            return;
        }

        currentComboType =
            FighterComboType.Assist;

        currentComboIndex = 0;

        nextNormalStepQueued = false;

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
    /// コンボ中の追加入力。
    /// </summary>
    private void ReadComboInput(
        FighterCommandData command
    )
    {
        if (currentComboType ==
            FighterComboType.Light)
        {
            if (command.lightAttackPressed)
            {
                QueueNextNormalStep();
            }
        }
        else if (currentComboType ==
                 FighterComboType.Heavy)
        {
            if (command.heavyAttackPressed)
            {
                QueueNextNormalStep();
            }
        }
    }

    /// <summary>
    /// 次段を予約する。
    /// </summary>
    private void QueueNextNormalStep()
    {
        NormalComboData combo =
            GetCurrentNormalCombo();

        if (combo == null)
        {
            return;
        }

        if (currentComboIndex >=
            combo.StepCount - 1)
        {
            return;
        }

        nextNormalStepQueued = true;

        Debug.Log(
            $"{name}：" +
            $"{currentComboType}コンボ " +
            $"{currentComboIndex + 2}段目予約",
            this
        );
    }

    /// <summary>
    /// MoveDataを実際に開始する。
    /// </summary>
    private void StartMoveInternal(
        MoveData move,
        int facingDirection
    )
    {
        if (move == null)
        {
            ResetCombo();
            return;
        }

        currentMove = move;
        currentMoveFrame = 0;

        attackFacingDirection =
            facingDirection >= 0 ? 1 : -1;

        if (attackHitbox != null)
        {
            attackHitbox.Deactivate();
        }

        if (stateMachine != null)
        {
            stateMachine.TryChangeState(
                FighterState.Attack
            );
        }

        Debug.Log(
            $"{name}：" +
            $"{move.MoveName}開始 " +
            $"Combo={currentComboType} " +
            $"段={currentComboIndex + 1}",
            this
        );
    }

    private void UpdateCurrentMove()
    {
        if (currentMove == null)
        {
            return;
        }

        UpdateAttackHitbox();

        // 弱・強コンボ
        if (TryAdvanceNormalCombo())
        {
            return;
        }

        // アシストコンボ
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


    private void UpdateAttackHitbox()
    {
        if (attackHitbox == null ||
            currentMove == null)
        {
            return;
        }

        bool shouldEnable =
            currentMove.IsActiveFrame(
                currentMoveFrame
            );

        if (shouldEnable)
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
        else
        {
            if (attackHitbox.IsActive)
            {
                attackHitbox.Deactivate();
            }
        }
    }

    /// <summary>
    /// 弱・強コンボの次段へ進む。
    /// </summary>
    private bool TryAdvanceNormalCombo()
    {
        if (currentComboType !=
                FighterComboType.Light &&
            currentComboType !=
                FighterComboType.Heavy)
        {
            return false;
        }

        if (!nextNormalStepQueued)
        {
            return false;
        }

        NormalComboData combo =
            GetCurrentNormalCombo();

        if (combo == null)
        {
            return false;
        }

        NormalComboStep currentStep =
            combo.GetStep(
                currentComboIndex
            );

        if (currentStep == null ||
            !currentStep.IsCancelWindow(
                currentMoveFrame
            ))
        {
            return false;
        }

        int nextIndex =
            currentComboIndex + 1;

        NormalComboStep nextStep =
            combo.GetStep(nextIndex);

        if (nextStep == null ||
            nextStep.Move == null)
        {
            return false;
        }

        currentComboIndex =
            nextIndex;

        nextNormalStepQueued =
            false;

        StartMoveInternal(
            nextStep.Move,
            attackFacingDirection
        );

        return true;
    }
    /// <summary>
    /// アシストコンボを自動で次段へ進める。
    /// </summary>
    private bool TryAdvanceAssistCombo()
    {
        if (currentComboType !=
            FighterComboType.Assist)
        {
            return false;
        }

        if (moveSet == null)
        {
            return false;
        }

        AssistComboData combo =
            moveSet.AssistCombo;

        if (combo == null)
        {
            return false;
        }

        // 最終段なら次へ行かない
        if (currentComboIndex >=
            combo.StepCount - 1)
        {
            return false;
        }

        AssistComboStep currentStep =
            combo.GetStep(
                currentComboIndex
            );

        if (currentStep == null)
        {
            return false;
        }

        // 自動で次段へ進むフレームまで待つ
        if (currentMoveFrame <
            currentStep.NextStartFrame)
        {
            return false;
        }

        int nextIndex =
            currentComboIndex + 1;

        AssistComboStep nextStep =
            combo.GetStep(nextIndex);

        if (nextStep == null ||
            nextStep.Move == null)
        {
            return false;
        }

        currentComboIndex =
            nextIndex;

        StartMoveInternal(
            nextStep.Move,
            attackFacingDirection
        );

        return true;
    }

    /// <summary>
    /// 現在実行している通常コンボを取得する。
    /// </summary>
    private NormalComboData GetCurrentNormalCombo()
    {
        if (moveSet == null)
        {
            return null;
        }

        switch (currentComboType)
        {
            case FighterComboType.Light:

                return moveSet.LightCombo;

            case FighterComboType.Heavy:

                return moveSet.HeavyCombo;

            default:

                return null;
        }
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

        currentMove = null;
        currentMoveFrame = 0;

        ResetCombo();

        if (stateMachine != null &&
            stateMachine.CurrentState !=
                FighterState.KO)
        {
            stateMachine.TryChangeState(
                FighterState.Idle
            );
        }
    }

    /// <summary>
    /// 被弾などで攻撃を強制終了する。
    /// </summary>
    public void CancelCurrentMove()
    {
        if (attackHitbox != null)
        {
            attackHitbox.Deactivate();
        }

        currentMove = null;
        currentMoveFrame = 0;

        ResetCombo();
    }

    /// <summary>
    /// コンボ状態を初期化する。
    /// </summary>
    private void ResetCombo()
    {
        currentComboType =
            FighterComboType.None;

        currentComboIndex = -1;

        nextNormalStepQueued = false;
    }

    /// <summary>
    /// 使用するキャラクターのMoveSetを変更する。
    /// </summary>
    public void SetMoveSet(
        FighterMoveSet newMoveSet
    )
    {
        CancelCurrentMove();

        moveSet = newMoveSet;
    }
}

