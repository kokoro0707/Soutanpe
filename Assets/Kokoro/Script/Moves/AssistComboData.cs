using System;
using UnityEngine;

/// <summary>
/// アシストコンボの進行方法。
/// </summary>
public enum AssistComboAdvanceMode
{
    // 同じボタンを押すたびに次の技へ進む
    PressEachStep,

    // 最初に1回押すだけで最後まで自動で進む
    OnePressAuto
}

/// <summary>
/// アシストコンボ1段分の設定。
/// </summary>
[Serializable]
public sealed class AssistComboStep
{
    [Header("使用する技")]
    [SerializeField]
    private MoveData move;

    [Header("次の技への移行受付")]
    [Tooltip("次の技へ移行できる開始フレーム")]
    [SerializeField, Min(0)]
    private int cancelStartFrame = 7;

    [Tooltip("次の技へ移行できる終了フレーム")]
    [SerializeField, Min(0)]
    private int cancelEndFrame = 15;

    public MoveData Move => move;

    public int CancelStartFrame =>
        cancelStartFrame;

    public int CancelEndFrame =>
        cancelEndFrame;

    /// <summary>
    /// 現在フレームが次の技への移行可能時間か。
    /// </summary>
    public bool IsCancelWindow(int currentFrame)
    {
        return currentFrame >= cancelStartFrame &&
               currentFrame <= cancelEndFrame;
    }

    public void Validate()
    {
        cancelStartFrame =
            Mathf.Max(0, cancelStartFrame);

        cancelEndFrame =
            Mathf.Max(
                cancelStartFrame,
                cancelEndFrame
            );
    }
}

/// <summary>
/// キャラクター1人分のアシストコンボ設定。
/// </summary>
[CreateAssetMenu(
    fileName = "AssistCombo_",
    menuName = "Fighting Game/Assist Combo"
)]
public sealed class AssistComboData : ScriptableObject
{
    [Header("コンボ進行方法")]
    [SerializeField]
    private AssistComboAdvanceMode advanceMode =
        AssistComboAdvanceMode.PressEachStep;

    [Header("コンボ内容")]
    [SerializeField]
    private AssistComboStep[] steps;

    public AssistComboAdvanceMode AdvanceMode =>
        advanceMode;

    public int StepCount =>
        steps == null ? 0 : steps.Length;

    public bool IsValid =>
        steps != null &&
        steps.Length > 0 &&
        steps[0] != null &&
        steps[0].Move != null;

    /// <summary>
    /// 指定されたコンボ段を取得する。
    /// </summary>
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
        {
            return;
        }

        foreach (AssistComboStep step in steps)
        {
            step?.Validate();
        }
    }
}
