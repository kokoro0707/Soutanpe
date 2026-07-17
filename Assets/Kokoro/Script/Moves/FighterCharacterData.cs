using UnityEngine;

/// <summary>
/// キャラクター固有の基本性能を保存する。
/// </summary>
[CreateAssetMenu(
    fileName = "Character_",
    menuName = "Fighting Game/Character Data"
)]
public sealed class FighterCharacterData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField]
    private string characterName;

    [Header("体力")]
    [SerializeField, Min(1)]
    private int maxHP = 1000;

    [Header("移動")]
    [SerializeField, Min(0f)]
    private float forwardWalkSpeed = 5f;

    [SerializeField, Min(0f)]
    private float backwardWalkSpeed = 3.5f;

    [SerializeField, Min(0f)]
    private float jumpPower = 12f;

    [SerializeField, Min(0f)]
    private float jumpHorizontalSpeed = 4f;

    [Header("技")]
    [SerializeField]
    private FighterMoveSet moveSet;

    public string CharacterName => characterName;
    public int MaxHP => maxHP;

    public float ForwardWalkSpeed => forwardWalkSpeed;
    public float BackwardWalkSpeed => backwardWalkSpeed;
    public float JumpPower => jumpPower;
    public float JumpHorizontalSpeed => jumpHorizontalSpeed;

    public FighterMoveSet MoveSet => moveSet;
}
