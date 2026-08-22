using System;
using UnityEngine;

[Serializable]
public sealed class NormalComboStep
{
    [Header("使用する技")]
    [SerializeField]
    private MoveData move;

    [Header("次の段への受付時間")]
    [SerializeField, Min(0)]
    private int cancelStartFrame = 10;

    [SerializeField, Min(0)]
    private int cancelEndFrame = 20;

    public MoveData Move => move;

    public int CancelStartFrame =>
        cancelStartFrame;

    public int CancelEndFrame =>
        cancelEndFrame;

    public bool IsCancelWindow(
        int currentFrame
    )
    {
        return
            currentFrame >= cancelStartFrame &&
            currentFrame <= cancelEndFrame;
    }

    public void Validate()
    {
        cancelStartFrame =
            Mathf.Max(
                0,
                cancelStartFrame
            );

        cancelEndFrame =
            Mathf.Max(
                cancelStartFrame,
                cancelEndFrame
            );
    }
}


[CreateAssetMenu(
    fileName = "NormalCombo_",
    menuName = "Fighting Game/Normal Combo"
)]
public sealed class NormalComboData :
    ScriptableObject
{
    [Header("コンボ内容")]
    [SerializeField]
    private NormalComboStep[] steps;

    public int StepCount =>
        steps == null
            ? 0
            : steps.Length;

    public bool IsValid =>
        steps != null &&
        steps.Length > 0 &&
        steps[0] != null &&
        steps[0].Move != null;

    public NormalComboStep GetStep(
        int index
    )
    {
        if (steps == null ||
            index < 0 ||
            index >= steps.Length)
        {
            return null;
        }

        return steps[index];
    }

    private void OnValidate()
    {
        if (steps == null)
        {
            return;
        }

        foreach (
            NormalComboStep step
            in steps
        )
        {
            step?.Validate();
        }
    }
}
