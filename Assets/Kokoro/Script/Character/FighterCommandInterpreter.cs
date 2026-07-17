using UnityEngine;

/// <summary>
/// 生の方向入力から、ガード・ステップ・ダッシュなどの
/// 格闘ゲーム用コマンドを生成する。
/// </summary>
public sealed class FighterCommandInterpreter : MonoBehaviour
{
    [Header("2回入力")]
    [Tooltip("1回目と2回目の入力を受け付ける最大フレーム")]
    [SerializeField, Min(1)]
    private int doubleTapWindowFrames = 18;

    [Tooltip("2回目の前入力を何フレーム保持したらダッシュにするか")]
    [SerializeField, Min(1)]
    private int dashHoldFrames = 4;

    private const int InvalidFrame = -1000000;

    private int lastForwardTapFrame = InvalidFrame;
    private int lastBackTapFrame = InvalidFrame;

    // 前・後ろへ変換した、前フレームの相対方向
    private int previousRelativeDirection;

    // 前入力の2回目を現在も押しているか
    private bool isHoldingSecondForwardInput;

    // 前入力の2回目を開始したフレーム
    private int secondForwardPressFrame = InvalidFrame;

    private bool hasFacingDirection;
    private int previousFacingDirection = 1;

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

        // 1：前、0：ニュートラル、-1：後ろ
        int relativeDirection =
            Mathf.Clamp(
                input.horizontal * facingDirection,
                -1,
                1
            );

        // 方向が前へ切り替わった瞬間
        bool forwardPressedThisFrame =
            relativeDirection == 1 &&
            previousRelativeDirection != 1;

        // 方向が後ろへ切り替わった瞬間
        bool backPressedThisFrame =
            relativeDirection == -1 &&
            previousRelativeDirection != -1;

        bool forwardStepPressed = false;
        bool backStepPressed = false;

        if (forwardPressedThisFrame)
        {
            int elapsedFrames =
                currentFrame - lastForwardTapFrame;

            if (elapsedFrames <= doubleTapWindowFrames)
            {
                // 前入力の2回目
                forwardStepPressed = true;

                isHoldingSecondForwardInput = true;
                secondForwardPressFrame = currentFrame;

                lastForwardTapFrame = InvalidFrame;
            }
            else
            {
                // 前入力の1回目
                lastForwardTapFrame = currentFrame;
            }
        }

        if (backPressedThisFrame)
        {
            int elapsedFrames =
                currentFrame - lastBackTapFrame;

            if (elapsedFrames <= doubleTapWindowFrames)
            {
                backStepPressed = true;
                lastBackTapFrame = InvalidFrame;
            }
            else
            {
                lastBackTapFrame = currentFrame;
            }
        }

        // 前入力を離したら、ダッシュ長押し判定を終了
        if (relativeDirection != 1)
        {
            isHoldingSecondForwardInput = false;
            secondForwardPressFrame = InvalidFrame;
        }

        bool dashHeld =
            isHoldingSecondForwardInput &&
            relativeDirection == 1 &&
            currentFrame - secondForwardPressFrame
                >= dashHoldFrames;

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

                // 相手と反対方向でガード
                guardHeld =
                    relativeDirection == -1,

                forwardStepPressed =
                    forwardStepPressed,

                backStepPressed =
                    backStepPressed,

                dashHeld =
                    dashHeld
            };

        previousRelativeDirection =
            relativeDirection;

        return command;
    }

    /// <summary>
    /// 自動振り向きが起きたとき、古い向きを基準にした
    /// 2回入力情報をリセットする。
    /// </summary>
    private void HandleFacingChange(
        int horizontal,
        int facingDirection
    )
    {
        if (!hasFacingDirection)
        {
            hasFacingDirection = true;
            previousFacingDirection = facingDirection;

            previousRelativeDirection =
                horizontal * facingDirection;

            return;
        }

        if (previousFacingDirection ==
            facingDirection)
        {
            return;
        }

        lastForwardTapFrame = InvalidFrame;
        lastBackTapFrame = InvalidFrame;

        isHoldingSecondForwardInput = false;
        secondForwardPressFrame = InvalidFrame;

        previousRelativeDirection =
            horizontal * facingDirection;

        previousFacingDirection =
            facingDirection;
    }
}
