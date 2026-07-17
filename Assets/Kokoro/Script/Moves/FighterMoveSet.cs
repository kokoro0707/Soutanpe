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
    [Header("通常技")]
    [SerializeField]
    private MoveData lightAttack;

    [SerializeField]
    private MoveData heavyAttack;

    [Header("必殺技")]
    [SerializeField]
    private MoveData specialAttack1;

    [SerializeField]
    private MoveData specialAttack2;

    [Header("アシストコンボ")]
    [SerializeField]
    private AssistComboData assistCombo;

    public MoveData LightAttack => lightAttack;
    public MoveData HeavyAttack => heavyAttack;

    public MoveData SpecialAttack1 =>
        specialAttack1;

    public MoveData SpecialAttack2 =>
        specialAttack2;

    public AssistComboData AssistCombo =>
        assistCombo;
}
