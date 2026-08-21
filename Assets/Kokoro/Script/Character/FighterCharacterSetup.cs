using UnityEngine;

/// <summary>
/// FighterCharacterDataを受け取り、
/// Fighterを構成する各Componentへ設定を反映する。
///
/// キャラクター選択画面からは、
/// 基本的にこのクラスへCharacterDataを渡すだけでよい。
/// </summary>
public sealed class FighterCharacterSetup :
    MonoBehaviour
{
    [Header("使用キャラクター")]
    [SerializeField]
    private FighterCharacterData characterData;

    [Header("参照")]
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

    public FighterCharacterData CharacterData =>
        characterData;

    private void Reset()
    {
        FindComponents();
    }

    private void Awake()
    {
        FindComponents();

        if (characterData != null)
        {
            ApplyCharacterData();
        }
    }

    /// <summary>
    /// 必要なComponentを自動取得する。
    /// </summary>
    private void FindComponents()
    {
        if (moveController == null)
        {
            moveController =
                GetComponent<FighterMoveController>();
        }

        if (grabController == null)
        {
            grabController =
                GetComponent<FighterGrabController>();
        }

        if (health == null)
        {
            health =
                GetComponent<FighterHealth>();
        }

        if (motor == null)
        {
            motor =
                GetComponent<FighterMotor>();
        }

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>(
                    true
                );
        }
    }

    /// <summary>
    /// CharacterDataをFighterへ反映する。
    /// </summary>
    public void ApplyCharacterData()
    {
        if (characterData == null)
        {
            Debug.LogWarning(
                $"{name}にCharacter Dataが設定されていません。",
                this
            );

            return;
        }

        // 技
        if (moveController != null)
        {
            moveController.SetMoveSet(
                characterData.MoveSet
            );
        }

        // つかみ
        if (grabController != null)
        {
            grabController.SetGrabData(
                characterData.GrabData
            );
        }

        // HP
        if (health != null)
        {
            health.SetMaxHP(
                characterData.MaxHP,
                true
            );
        }

        // 移動性能
        if (motor != null)
        {
            motor.SetMovementStats(
                characterData.ForwardWalkSpeed,
                characterData.BackwardWalkSpeed,
                characterData.JumpPower,
                characterData.JumpHorizontalSpeed
            );
        }

        if (motor != null)
        {
            motor.SetSpecialMovementStats(
                characterData.ForwardStepSpeed,
                characterData.ForwardStepFrames,
                characterData.BackStepSpeed,
                characterData.BackStepFrames,
                characterData.DashSpeed
            );
        }


        // Animator
        if (animator != null &&
            characterData.AnimatorController != null)
        {
            animator.runtimeAnimatorController =
                characterData.AnimatorController;
        }

        Debug.Log(
            $"{name}：" +
            $"{characterData.CharacterName}を設定しました。",
            this
        );
    }

    /// <summary>
    /// キャラクター選択後などに、
    /// CharacterDataを変更する。
    /// </summary>
    public void SetCharacterData(
        FighterCharacterData newCharacterData
    )
    {
        if (newCharacterData == null)
        {
            Debug.LogWarning(
                $"{name}へnullのCharacterDataが渡されました。",
                this
            );

            return;
        }

        characterData =
            newCharacterData;

        ApplyCharacterData();
    }
}
