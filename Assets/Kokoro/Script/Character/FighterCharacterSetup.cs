using UnityEngine;

public sealed class FighterCharacterSetup : MonoBehaviour
{
    [SerializeField]
    private FighterCharacterData characterData;

    [SerializeField]
    private FighterMoveController moveController;

    [SerializeField]
    private FighterGrabController grabController;

    [SerializeField]
    private FighterHealth health;

    [SerializeField]
    private FighterMotor motor;

    [SerializeField]
    private Animator animator;


    public void SetCharacterData(
        FighterCharacterData data
    )
    {
        if (data == null)
        {
            Debug.LogError(
                $"{name} : CharacterDataがNULL"
            );

            return;
        }

        characterData = data;

        Debug.Log(
            $"【FighterCharacterSetup】" +
            $"{name} に {data.name} を設定"
        );

        ApplyCharacterData();
    }



    private void ApplyCharacterData()
    {
        if (characterData == null)
        {
            return;
        }

        // 技
        moveController?.SetMoveSet(
            characterData.MoveSet
        );

        // 投げ
        grabController?.SetGrabData(
            characterData.GrabData
        );

        // HP
        health?.SetMaxHP(
            characterData.MaxHP,
            true
        );

        // 基本移動
        motor?.SetMovementStats(
            characterData.ForwardWalkSpeed,
            characterData.BackwardWalkSpeed,
            characterData.JumpPower,
            characterData.JumpHorizontalSpeed
        );

        // ステップ・ダッシュ
        motor?.SetSpecialMovementStats(
            characterData.ForwardStepSpeed,
            characterData.ForwardStepFrames,
            characterData.BackStepSpeed,
            characterData.BackStepFrames,
            characterData.DashSpeed
        );

        // キャラ専用Animator

        if (animator != null)
        {
            Debug.Log(
                $"【Animator変更】" +
                $"{name} → " +
                $"{characterData.AnimatorController?.name}"
            );

            animator.runtimeAnimatorController =
                characterData.AnimatorController;

            animator.Rebind();
            animator.Update(0f);
        }
        else
        {
            Debug.LogError(
                $"{name} : Animator参照がありません"
            );
        }


        Debug.Log(
            $"{name} に " +
            $"{characterData.name} を適用しました",
            this
        );
    }
}
