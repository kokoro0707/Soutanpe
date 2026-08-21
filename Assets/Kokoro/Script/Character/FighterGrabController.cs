using UnityEngine;

public sealed class FighterGrabController :
    MonoBehaviour
{
    private enum GrabPhase
    {
        None,
        Attempt,
        Holding,
        Recovery
    }

    [Header("つかみデータ")]
    [SerializeField]
    private GrabData grabData;

    [Header("参照")]
    [SerializeField]
    private GrabHitbox grabHitbox;

    [SerializeField]
    private FighterStateMachine stateMachine;

    [SerializeField]
    private FighterMotor motor;

    [SerializeField]
    private FighterMoveController moveController;

    private GrabPhase phase =
        GrabPhase.None;

    private FighterGrabTarget grabbedTarget;

    private int currentFrame;
    private int grabDirection = 1;

    public bool IsBusy =>
        phase != GrabPhase.None;

    private void Reset()
    {
        stateMachine =
            GetComponent<FighterStateMachine>();

        motor =
            GetComponent<FighterMotor>();

        moveController =
            GetComponent<FighterMoveController>();

        grabHitbox =
            GetComponentInChildren<GrabHitbox>(
                true
            );
    }

    private void Awake()
    {
        if (stateMachine == null)
            stateMachine = GetComponent<FighterStateMachine>();

        if (motor == null)
            motor = GetComponent<FighterMotor>();

        if (moveController == null)
            moveController = GetComponent<FighterMoveController>();

        if (grabHitbox == null)
        {
            grabHitbox =
                GetComponentInChildren<GrabHitbox>(
                    true
                );
        }

        grabHitbox?.Deactivate();
    }

    public void SimulateCommand(
        FighterCommandData command,
        int facingDirection,
        bool isGrounded
    )
    {
        if (phase == GrabPhase.None)
        {
            if (command.grabPressed &&
                isGrounded &&
                stateMachine != null &&
                stateMachine.CanStartGrab)
            {
                StartGrab(
                    facingDirection
                );
            }

            return;
        }

        switch (phase)
        {
            case GrabPhase.Attempt:
                UpdateAttempt();
                break;

            case GrabPhase.Holding:
                UpdateHolding();
                break;

            case GrabPhase.Recovery:
                UpdateRecovery();
                break;
        }
    }

    private void StartGrab(
        int facingDirection
    )
    {
        if (grabData == null)
        {
            Debug.LogWarning(
                $"{name}にGrab Dataがありません。",
                this
            );

            return;
        }

        grabDirection =
            facingDirection >= 0 ? 1 : -1;

        currentFrame = 0;

        phase =
            GrabPhase.Attempt;

        moveController?.CancelCurrentMove();
        motor?.CancelSpecialMovement();

        stateMachine.ForceChangeState(
            FighterState.Grab
        );

        Debug.Log(
            $"{name}：つかみ開始",
            this
        );
    }
    private void UpdateAttempt()
    {
        bool active =
            currentFrame >=
                grabData.StartupFrames &&
            currentFrame <
                grabData.StartupFrames +
                grabData.ActiveFrames;

        if (active)
        {
            if (!grabHitbox.IsActive)
            {
                grabHitbox.Activate(
                    grabData,
                    grabDirection,
                    this
                );
            }
        }
        else if (grabHitbox.IsActive)
        {
            grabHitbox.Deactivate();
        }

        currentFrame++;

        int totalFrames =
            grabData.StartupFrames +
            grabData.ActiveFrames +
            grabData.WhiffRecoveryFrames;

        if (currentFrame >= totalFrames)
        {
            EndGrab();
        }
    }

    public void TryGrab(
        FighterGrabTarget target
    )
    {
        if (phase != GrabPhase.Attempt ||
            target == null ||
            grabbedTarget != null)
        {
            return;
        }

        if (!target.TryBeginGrab())
        {
            return;
        }

        grabbedTarget =
            target;

        grabHitbox.Deactivate();

        phase =
            GrabPhase.Holding;

        currentFrame = 0;

        UpdateTargetPosition();

        Debug.Log(
            $"{name}：つかみ成功",
            this
        );
    }

    private void UpdateHolding()
    {
        if (grabbedTarget == null)
        {
            EndGrab();
            return;
        }

        UpdateTargetPosition();

        currentFrame++;

        if (currentFrame >=
            grabData.HoldFrames)
        {
            ThrowTarget();
        }
    }

    private void UpdateTargetPosition()
    {
        if (grabbedTarget == null)
        {
            return;
        }

        Vector2 offset =
            grabData.HoldOffset;

        offset.x =
            Mathf.Abs(offset.x) *
            grabDirection;

        Vector2 targetPosition =
            (Vector2)transform.position +
            offset;

        grabbedTarget.SetGrabPosition(
            targetPosition
        );
    }

    private void ThrowTarget()
    {
        // 投げアニメーション状態へ
        if (stateMachine != null)
        {
            stateMachine.ForceChangeState(
                FighterState.Throw
            );
        }

        if (grabbedTarget != null)
        {
            grabbedTarget.Throw(
                grabData,
                grabDirection
            );

            grabbedTarget = null;
        }

        phase =
            GrabPhase.Recovery;

        currentFrame = 0;

        Debug.Log(
            $"{name}：投げ",
            this
        );
    }


    private void UpdateRecovery()
    {
        currentFrame++;

        if (currentFrame >=
            grabData.ThrowRecoveryFrames)
        {
            EndGrab();
        }
    }

    private void EndGrab()
    {
        grabHitbox?.Deactivate();

        grabbedTarget = null;

        currentFrame = 0;

        phase =
            GrabPhase.None;

        if (stateMachine != null &&
            stateMachine.CurrentState !=
                FighterState.KO)
        {
            stateMachine.ForceChangeState(
                FighterState.Idle
            );
        }
    }

    public void CancelGrab()
    {
        if (grabbedTarget != null)
        {
            grabbedTarget
                .CancelBeingGrabbed();

            grabbedTarget = null;
        }

        grabHitbox?.Deactivate();

        phase =
            GrabPhase.None;

        currentFrame = 0;
    }

    /// <summary>
    /// 使用するつかみデータを変更する。
    /// </summary>
    public void SetGrabData(
        GrabData newGrabData
    )
    {
        CancelGrab();

        grabData =
            newGrabData;
    }

}
