using UnityEngine;

/// <summary>
/// キャラクター1人分の技構成をまとめる。
/// </summary>
[CreateAssetMenu(
    fileName = "MoveSet_",
    menuName = "Fighting Game/Move Set"
)]
public sealed class FighterMoveSet : ScriptableObject
{
    [Header("弱コンボ")]
    [SerializeField]
    private NormalComboData lightCombo;

    [Header("強コンボ")]
    [SerializeField]
    private NormalComboData heavyCombo;

    [Header("アシストコンボ")]
    [SerializeField]
    private AssistComboData assistCombo;

    [Header("必殺技")]
    [SerializeField]
    private MoveData forwardSpecial;

    [SerializeField]
    private MoveData downSpecial;

    [Header("空中攻撃")]
    [SerializeField]
    private MoveData jumpAttack;




    public NormalComboData LightCombo =>
        lightCombo;

    public NormalComboData HeavyCombo =>
        heavyCombo;

    public AssistComboData AssistCombo =>
        assistCombo;

    public MoveData ForwardSpecial =>
    forwardSpecial;

    public MoveData DownSpecial =>
        downSpecial;

    public MoveData JumpAttack =>
        jumpAttack;
}
