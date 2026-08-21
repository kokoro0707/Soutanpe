using UnityEngine;

public sealed class FighterCommandInterpreter :
    MonoBehaviour
{
    [Header("2‰ñ“ü—Í")]
    [SerializeField, Min(1)]
    private int doubleTapWindowFrames = 18;

    [SerializeField, Min(1)]
    private int dashHoldFrames = 4;

    private const int InvalidFrame = -1000000;

    private int lastForwardTapFrame = InvalidFrame;
    private int lastBackTapFrame = InvalidFrame;

    private int previousRelativeDirection;

    private bool isHoldingSecondForwardInput;

    private int secondForwardPressFrame =
        InvalidFrame;

    private bool hasFacingDirection;
    private int previousFacingDirection = 1;

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

        int relativeDirection =
            Mathf.Clamp(
                input.horizontal *
                facingDirection,
                -1,
                1
            );

        bool forwardPressedThisFrame =
            relativeDirection == 1 &&
            previousRelativeDirection != 1;

        bool backPressedThisFrame =
            relativeDirection == -1 &&
            previousRelativeDirection != -1;

        bool forwardStepPressed = false;
        bool backStepPressed = false;

        if (forwardPressedThisFrame)
        {
            int elapsedFrames =
                currentFrame -
                lastForwardTapFrame;

            if (elapsedFrames <=
                doubleTapWindowFrames)
            {
                forwardStepPressed = true;

                isHoldingSecondForwardInput =
                    true;

                secondForwardPressFrame =
                    currentFrame;

                lastForwardTapFrame =
                    InvalidFrame;
            }
            else
            {
                lastForwardTapFrame =
                    currentFrame;
            }
        }

        if (backPressedThisFrame)
        {
            int elapsedFrames =
                currentFrame -
                lastBackTapFrame;

            if (elapsedFrames <=
                doubleTapWindowFrames)
            {
                backStepPressed = true;

                lastBackTapFrame =
                    InvalidFrame;
            }
            else
            {
                lastBackTapFrame =
                    currentFrame;
            }
        }

        if (relativeDirection != 1)
        {
            isHoldingSecondForwardInput =
                false;

            secondForwardPressFrame =
                InvalidFrame;
        }

        bool dashHeld =
            isHoldingSecondForwardInput &&
            relativeDirection == 1 &&
            currentFrame -
                secondForwardPressFrame >=
                dashHoldFrames;

        bool downSpecialPressed =
            input.specialAttackPressed &&
            input.vertical == -1;

        bool forwardSpecialPressed =
            input.specialAttackPressed &&
            input.vertical != -1 &&
            relativeDirection == 1;

        FighterCommandData command =
            new FighterCommandData
            {
                horizontal =
                    input.horizontal,

                vertical =
                    input.vertical,

                jumpPressed =
                    input.jumpPressed,

                lightAttackPressed =
                    input.lightAttackPressed,

                heavyAttackPressed =
                    input.heavyAttackPressed,

                assistComboPressed =
                    input.assistComboPressed,

                guardHeld =
                    relativeDirection == -1,

                forwardStepPressed =
                    forwardStepPressed,

                backStepPressed =
                    backStepPressed,

                forwardSpecialPressed=
                    forwardSpecialPressed,

                downSpecialPressed=
                    downSpecialPressed,

                grabPressed=
                    input.grabPressed,

                dashHeld =
                    dashHeld
            };

        previousRelativeDirection =
            relativeDirection;

        return command;
    }

    private void HandleFacingChange(
        int horizontal,
        int facingDirection
    )
    {
        if (!hasFacingDirection)
        {
            hasFacingDirection = true;

            previousFacingDirection =
                facingDirection;

            previousRelativeDirection =
                horizontal *
                facingDirection;

            return;
        }

        if (previousFacingDirection ==
            facingDirection)
        {
            return;
        }

        lastForwardTapFrame =
            InvalidFrame;

        lastBackTapFrame =
            InvalidFrame;

        isHoldingSecondForwardInput =
            false;

        secondForwardPressFrame =
            InvalidFrame;

        previousRelativeDirection =
            horizontal *
            facingDirection;

        previousFacingDirection =
            facingDirection;
    }
}
