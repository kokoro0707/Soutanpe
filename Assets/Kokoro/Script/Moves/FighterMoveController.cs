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

    [Header("横必殺技の判定表示")]
    [SerializeField]
    private GameObject forwardSpecialHitVisual;

    [Header("参照")]
    [SerializeField]
    private AttackHitbox attackHitbox;

    [Header("コンボリセット")]
    [SerializeField, Range(0.1f, 1f)]
    private float comboResetDamageRate = 0.8f;

    [SerializeField, Range(0.1f, 1f)]
    private float minimumDamageRate = 0.5f;

    private int comboResetCount;

    [Header("SP")]
    [SerializeField]
    private FighterSPGauge spGauge;

    [Header("コンボリセットSP")]
    [SerializeField, Min(0)]
    private int comboResetSPCost = 1;

    [Header("SP攻撃 貫通")]
    [SerializeField]
    private Transform opponentRoot;

    private Collider2D[] ownColliders;
    private Collider2D[] opponentColliders;

    private bool isIgnoringOpponentCollision;

    private bool attackLaunchUsed;



    [SerializeField]
    private FighterHealth ownerHealth;

    [SerializeField]
    private FighterStateMachine stateMachine;

    [SerializeField]
    private FighterMotor motor;

    [SerializeField]
    private SPCinematicController spCinematic;

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

    private GameObject currentMoveEffect;

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

        motor = 
            GetComponent<FighterMotor>();
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

        if (forwardSpecialHitVisual != null)
        {
            forwardSpecialHitVisual.SetActive(false);
        }


        if (attackHitbox != null)
        {
            attackHitbox.Deactivate();
        }

        if (spGauge == null)
        {
            spGauge = GetComponent<FighterSPGauge>();
        }

        if (motor == null)
        {
            motor = GetComponent<FighterMotor>();
        }

        ownColliders = GetComponentsInChildren<Collider2D>(true);

    }

    public float CurrentDamageMultiplier
    {
        get
        {
            float multiplier =
                Mathf.Pow(
                    comboResetDamageRate,
                    comboResetCount
                );

            return Mathf.Max(
                    minimumDamageRate,
                    multiplier
            );
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
        if (isGrounded)
        {
            airAttackUsed = false;
        }


        if (TryComboReset(command))
        {
            return;
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

        if (command.comboResetPressed)
        {
            Debug.Log(
                $"{name}：LB入力 " +
                $"Combo={currentComboType} " +
                $"段={currentComboIndex} " +
                $"Frame={currentMoveFrame} " +
                $"SP={(spGauge != null ? spGauge.CurrentSP : -1)}",
                this
            );
        }

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

        if (command.spAttackPressed)
        {
            StartSPAttack(
                moveSet.SPAttack,
                facingDirection);

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

    private void StartSPAttack(
        MoveData move,
        int facingDirection
    )
    {
        if (move == null)
        {
            Debug.LogWarning(
                $"{name}のSP攻撃が設定されていません。",
                this
            );

            return;
        }

        if (spGauge == null)
        {
            Debug.LogWarning(
                $"{name}にSPGaugeがありません。",
                this
            );

            return;
        }

        // SP消費
        if (!spGauge.TryConsume(move.SPCost))
        {
            Debug.Log(
                $"{name}：SP不足 " +
                $"必要SP={move.SPCost} " +
                $"現在SP={spGauge.CurrentSP}",
                this
            );

            return;
        }

        ResetCombo();


        // =========================
        // SPAttackを先に開始
        // =========================

        StartMoveInternal(
            move,
            facingDirection
        );


        // 相手を貫通可能にする
        StartOpponentPassThrough();


        // =========================
        // アニメ開始後に暗転停止
        // =========================

        if (spCinematic != null)
        {
            StartCoroutine(
                spCinematic.PlaySPFreeze()
            );
        }


        Debug.Log(
            $"{name}：SP攻撃 {move.MoveName} " +
            $"残りSP={spGauge.CurrentSP}",
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
        // ★前の技の特殊移動を解除
        motor?.ClearAttackMoveVelocity();

        attackLaunchUsed = false;

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

        UpdateAttackMovement();

        UpdateAttackHitbox();

        UpdateMoveEffect();

        //UpdateForwardSpecialVisual();

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
                    ownerHealth,
                    CurrentDamageMultiplier
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
        HideMoveEffect();
        // ★SP攻撃だったかを先に保存
        bool wasSPAttack =
            moveSet != null &&
            currentMove == moveSet.SPAttack;


        EndOpponentPassThrough();

        motor?.ClearAttackMoveVelocity();

        if (attackHitbox != null)
        {
            attackHitbox.Deactivate();
        }

        currentMove = null;
        currentMoveFrame = 0;

        ResetCombo();

        comboResetCount = 0;

        if (stateMachine != null &&
            stateMachine.CurrentState != FighterState.KO)
        {
            stateMachine.TryChangeState(
                FighterState.Idle
            );
        }

        if (forwardSpecialHitVisual != null)
        {
            forwardSpecialHitVisual.SetActive(false);
        }


        // =========================
        // ★SP攻撃終了
        // =========================

        if (wasSPAttack &&
            spCinematic != null)
        {
            spCinematic.EndSPCinematic();
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

        if (forwardSpecialHitVisual != null)
        {
            forwardSpecialHitVisual.SetActive(false);
        }

        EndOpponentPassThrough() ;
        motor?.ClearAttackMoveVelocity();

        currentMove = null;
        currentMoveFrame = 0;

        ResetCombo();

        comboResetCount = 0;
        HideMoveEffect();
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

    private void UpdateForwardSpecialVisual()
    {
        if (forwardSpecialHitVisual == null)
        {
            return;
        }

        bool shouldShow = false;

        // 今使っている技が横必殺技か
        if (currentMove != null &&
            moveSet != null &&
            currentMove == moveSet.ForwardSpecial)
        {
            // Active Frames中だけ表示
            shouldShow =
                currentMove.IsActiveFrame(
                    currentMoveFrame
                );
        }

        forwardSpecialHitVisual.SetActive(
            shouldShow
        );
    }

    private bool CanUseComboReset()
    {
        if (currentMove == null)
        {
            return false;
        }

        // 弱・強コンボだけ
        if (currentComboType != FighterComboType.Light &&
            currentComboType != FighterComboType.Heavy)
        {
            return false;
        }

        // 最終段のRecovery中だけ
        return currentMove.IsRecoveryFrame(
            currentMoveFrame
        );
    }
    private bool TryComboReset(
     FighterCommandData command
 )
    {
        // LBを押していない
        if (!command.comboResetPressed)
        {
            return false;
        }

        Debug.Log($"{name}：LB入力を確認");

        // =========================
        // SPゲージ確認
        // =========================

        if (spGauge == null)
        {
            Debug.LogWarning(
                $"{name}：SPGaugeが設定されていません",
                this
            );

            return false;
        }

        Debug.Log(
            $"{name}：消費前SP={spGauge.CurrentSP}"
        );

        // =========================
        // LBを押した時点でSP消費
        // =========================

        if (!spGauge.TryConsume(comboResetSPCost))
        {
            Debug.Log(
                $"{name}：SP不足 " +
                $"SP={spGauge.CurrentSP}/{spGauge.MaxSP}",
                this
            );

            return false;
        }

        Debug.Log(
            $"{name}：SP消費成功 " +
            $"SP={spGauge.CurrentSP}/{spGauge.MaxSP}"
        );

        // =========================
        // リセット成功判定
        // =========================

        if (!CanUseComboReset())
        {
            Debug.Log(
                $"{name}：コンボリセット失敗 " +
                $"SPは消費済み",
                this
            );

            return false;
        }

        // =========================
        // コンボリセット成功
        // =========================

        attackHitbox?.Deactivate();

        motor?.ClearAttackMoveVelocity();

        currentMove = null;
        currentMoveFrame = 0;

        ResetCombo();

        comboResetCount++;

        if (stateMachine != null &&
            stateMachine.CurrentState != FighterState.KO)
        {
            stateMachine.ForceChangeState(
                FighterState.Idle
            );
        }

        Debug.Log(
            $"{name}：コンボリセット成功 " +
            $"SP={spGauge.CurrentSP}/{spGauge.MaxSP} " +
            $"補正={CurrentDamageMultiplier:P0}",
            this
        );

        return true;
    }

    private void UpdateAttackMovement()
    {
        if (motor == null)
        {
            return;
        }

        if (currentMove == null)
        {
            motor.ClearAttackMoveVelocity();
            return;
        }

        // 移動フレーム中
        if (currentMove.IsMoveFrame(currentMoveFrame))
        {
            float velocityX =
                currentMove.MoveSpeed *
                attackFacingDirection;

            motor.SetAttackMoveVelocity(
                velocityX
            );
        }
        else
        {
            motor.ClearAttackMoveVelocity();
        }

        // =========================
        // ★昇竜拳などの上方向移動
        // =========================

        if (currentMove.UseLaunch &&
            !attackLaunchUsed &&
            currentMoveFrame >=
                currentMove.LaunchFrame)
        {
            motor.ApplyAttackLaunch(
                currentMove.LaunchVelocityY
            );

            attackLaunchUsed = true;
        }

    }
    /// <summary>
    /// SP攻撃中、相手の本体Colliderを貫通できるようにする。
    /// </summary>
    private void StartOpponentPassThrough()
    {
        if (opponentRoot == null)
        {
            Debug.LogWarning(
                $"{name}：Opponent Rootが設定されていません。",
                this
            );

            return;
        }

        if (ownColliders == null ||
            ownColliders.Length == 0)
        {
            ownColliders =
                GetComponentsInChildren<Collider2D>(true);
        }

        opponentColliders =
            opponentRoot.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D own in ownColliders)
        {
            if (own == null)
            {
                continue;
            }

            foreach (Collider2D opponent in opponentColliders)
            {
                if (opponent == null)
                {
                    continue;
                }

                // Triggerは物理的に邪魔しないので無視
                if (own.isTrigger ||
                    opponent.isTrigger)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(
                    own,
                    opponent,
                    true
                );
            }
        }

        isIgnoringOpponentCollision = true;

        Debug.Log(
            $"{name}：SP攻撃 貫通開始",
            this
        );
    }



    /// <summary>
    /// 相手との物理衝突を元に戻す。
    /// </summary>
    private void EndOpponentPassThrough()
    {
        if (!isIgnoringOpponentCollision)
        {
            return;
        }

        if (ownColliders != null &&
            opponentColliders != null)
        {
            foreach (Collider2D own in ownColliders)
            {
                if (own == null)
                {
                    continue;
                }

                foreach (Collider2D opponent in opponentColliders)
                {
                    if (opponent == null)
                    {
                        continue;
                    }

                    if (own.isTrigger ||
                        opponent.isTrigger)
                    {
                        continue;
                    }

                    Physics2D.IgnoreCollision(
                        own,
                        opponent,
                        false
                    );
                }
            }
        }

        isIgnoringOpponentCollision = false;

        Debug.Log(
            $"{name}：SP攻撃 貫通終了",
            this
        );
    }



    private void UpdateMoveEffect()
    {
        if (currentMove == null)
        {
            HideMoveEffect();
            return;
        }

        // ActiveFrame以外は消す
        if (!currentMove.IsActiveFrame(currentMoveFrame))
        {
            HideMoveEffect();
            return;
        }

        // エフェクトが設定されていない技
        if (currentMove.EffectPrefab == null)
        {
            return;
        }

        // すでに出ているなら作り直さない
        if (currentMoveEffect != null)
        {
            return;
        }

        Vector2 offset =
            currentMove.EffectOffset;

        // 左向きならXを反転
        offset.x *= attackFacingDirection;

        Vector3 position =
            transform.position +
            new Vector3(
                offset.x,
                offset.y,
                0f
            );

        currentMoveEffect =
            Instantiate(
                currentMove.EffectPrefab,
                position,
                Quaternion.identity,
                transform
            );

        // 左向きならエフェクトも左右反転
        Vector3 scale =
            currentMoveEffect.transform.localScale;

        scale.x =
            Mathf.Abs(scale.x) *
            attackFacingDirection;

        currentMoveEffect.transform.localScale =
            scale;
    }

    private void HideMoveEffect()
    {
        if (currentMoveEffect == null)
        {
            return;
        }

        Destroy(currentMoveEffect);

        currentMoveEffect = null;
    }


}