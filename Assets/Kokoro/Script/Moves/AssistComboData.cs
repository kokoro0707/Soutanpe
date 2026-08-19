using System;
using UnityEngine;

/// <summary>
/// アシストコンボの1段分。
/// </summary>
[Serializable]
public sealed class AssistComboStep
{
    [Header("使用する技")]
    [SerializeField]
    private MoveData move;

    [Header("次の技を自動で出すタイミング")]
    [SerializeField, Min(0)]
    private int nextStartFrame = 10;

    public MoveData Move => move;
    public int NextStartFrame => nextStartFrame;

    public void Validate()
    {
        nextStartFrame =
            Mathf.Max(0, nextStartFrame);
    }
}

/// <summary>
/// 専用ボタンを1回押すことで、
/// 自動的に最後まで進むアシストコンボ。
/// </summary>
[CreateAssetMenu(
    fileName = "AssistCombo_",
    menuName = "Fighting Game/Assist Combo"
)]
public sealed class AssistComboData : ScriptableObject
{
    [Header("アシストコンボ内容")]
    [SerializeField]
    private AssistComboStep[] steps;

    public int StepCount =>
        steps == null ? 0 : steps.Length;

    public bool IsValid =>
        steps != null &&
        steps.Length > 0 &&
        steps[0] != null &&
        steps[0].Move != null;

    public AssistComboStep GetStep(int index)
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
            return;

        foreach (AssistComboStep step in steps)
        {
            step?.Validate();
        }
    }
}
