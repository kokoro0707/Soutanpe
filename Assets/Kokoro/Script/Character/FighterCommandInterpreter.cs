using UnityEngine;

/// <summary>
/// 生入力を格ゲー用コマンドへ変換する。
/// フレーム数を使用するため、オンライン同期にも対応させやすい。
/// </summary>
public sealed class FighterCommandInterpreter : MonoBehaviour
{
    [Header("2回入力設定")]
    [Tooltip("1回目から2回目までに許されるフレーム数")]
    [SerializeField, Min(1)]
    private int doubleTapWindowFrames = 12;

    [Tooltip("2回目を何フレーム押し続けたらダッシュにするか")]
    [SerializeField, Min(1)]
    private int dashHoldFrames = 6;

    private const int InvalidFrame = -1000000;

    private int previousHorizontal;

    private int lastForwardTapFrame = InvalidFrame;
    private int lastBackTapFrame = InvalidFrame;

    private bool isSecondForwardPressHeld;
    private int secondForwardPressFrame = InvalidFrame;

    private bool hasFacingDirection;
    private int previousFacingDirection;

    /// <summary>
    /// 1フレーム分の入力を格ゲー用コマンドへ変換する。
    /// </summary>
    public FighterCommandData BuildCommand(
        FighterInputData input,
        int currentFrame,
        int facingDirection
    )
    {
        facingDirection =
            facingDirection >= 0 ? 1 : -1;

        HandleFacingChange(
            input.horizontal,
            facingDirection
        );

        // 右向きで右入力なら前、左向きで左入力なら前。
        int relativeDirection =
            input.horizontal * facingDirection;

        bool horizontalPressedThisFrame =
            input.horizontal != 0 &&
            previousHorizontal == 0;

        bool forwardStepPressed = false;
        bool backStepPressed = false;

        if (horizontalPressedThisFrame)
        {
            if (relativeDirection > 0)
            {
                forwardStepPressed =
                    CheckForwardDoubleTap(currentFrame);
            }
            else if (relativeDirection < 0)
            {
                backStepPressed =
                    CheckBackDoubleTap(currentFrame);
            }
        }

        bool forwardHeld =
            relativeDirection > 0;

        bool dashHeld =
            isSecondForwardPressHeld &&
            forwardHeld &&
            currentFrame - secondForwardPressFrame
                >= dashHoldFrames;

        // 2回目の前入力を離したらダッシュ判定を終了する
        if (!forwardHeld)
        {
            isSecondForwardPressHeld = false;
        }

        FighterCommandData command =
            new FighterCommandData
            {
                horizontal = input.horizontal,
                vertical = input.vertical,

                jumpPressed = input.jumpPressed,
                lightAttackPressed =
                    input.lightAttackPressed,
                heavyAttackPressed =
                    input.heavyAttackPressed,

                // 相手と反対方向を入力している
                guardHeld =
                    relativeDirection < 0,

                forwardStepPressed =
                    forwardStepPressed,

                backStepPressed =
                    backStepPressed,

                dashHeld =
                    dashHeld
            };

        previousHorizontal = input.horizontal;

        return command;
    }

    private bool CheckForwardDoubleTap(
        int currentFrame
    )
    {
        bool isDoubleTap =
            currentFrame - lastForwardTapFrame
                <= doubleTapWindowFrames;

        if (!isDoubleTap)
        {
            lastForwardTapFrame = currentFrame;
            return false;
        }

        lastForwardTapFrame = InvalidFrame;

        isSecondForwardPressHeld = true;
        secondForwardPressFrame = currentFrame;

        return true;
    }

    private bool CheckBackDoubleTap(
        int currentFrame
    )
    {
        bool isDoubleTap =
            currentFrame - lastBackTapFrame
                <= doubleTapWindowFrames;

        if (!isDoubleTap)
        {
            lastBackTapFrame = currentFrame;
            return false;
        }

        lastBackTapFrame = InvalidFrame;

        return true;
    }

    /// <summary>
    /// 自動振り向きが発生したときに、
    /// 古い向きを基準にした2回入力をリセットする。
    /// </summary>
    private void HandleFacingChange(
        int currentHorizontal,
        int facingDirection
    )
    {
        if (!hasFacingDirection)
        {
            hasFacingDirection = true;
            previousFacingDirection = facingDirection;
            return;
        }

        if (previousFacingDirection == facingDirection)
        {
            return;
        }

        lastForwardTapFrame = InvalidFrame;
        lastBackTapFrame = InvalidFrame;

        isSecondForwardPressHeld = false;
        secondForwardPressFrame = InvalidFrame;

        // 振り向いた瞬間の入力を新しい1回目として
        // 誤認識しないようにする
        previousHorizontal = currentHorizontal;

        previousFacingDirection = facingDirection;
    }
}
