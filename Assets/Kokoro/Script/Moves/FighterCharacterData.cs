using UnityEngine;

/// <summary>
/// キャラクター1人分の設定をまとめるデータ。
/// キャラクター選択では基本的にこのデータだけを渡す。
/// </summary>
[CreateAssetMenu(
    fileName = "Character_",
    menuName = "Fighting Game/Character Data"
)]
public sealed class FighterCharacterData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField]
    private string characterName = "Character";

    [Header("体力")]
    [SerializeField, Min(1)]
    private int maxHP = 1000;

    [Header("移動性能")]
    [SerializeField, Min(0f)]
    private float forwardWalkSpeed = 5f;

    [SerializeField, Min(0f)]
    private float backwardWalkSpeed = 3.5f;

    [SerializeField, Min(0f)]
    private float jumpPower = 12f;

    [SerializeField, Min(0f)]
    private float jumpHorizontalSpeed = 4f;

    [Header("ステップ・ダッシュ")]
    [SerializeField, Min(0f)]
    private float forwardStepSpeed = 9f;

    [SerializeField, Min(1)]
    private int forwardStepFrames = 10;

    [SerializeField, Min(0f)]
    private float backStepSpeed = 8f;

    [SerializeField, Min(1)]
    private int backStepFrames = 12;

    [SerializeField, Min(0f)]
    private float dashSpeed = 7f;


    [Header("技")]
    [SerializeField]
    private FighterMoveSet moveSet;

    [Header("つかみ")]
    [SerializeField]
    private GrabData grabData;

    [Header("アニメーション")]
    [SerializeField]
    private RuntimeAnimatorController animatorController;

    public string CharacterName =>
        characterName;

    public int MaxHP =>
        maxHP;

    public float ForwardWalkSpeed =>
        forwardWalkSpeed;

    public float BackwardWalkSpeed =>
        backwardWalkSpeed;

    public float JumpPower =>
        jumpPower;

    public float JumpHorizontalSpeed =>
        jumpHorizontalSpeed;

    public FighterMoveSet MoveSet =>
        moveSet;

    public GrabData GrabData =>
        grabData;

    public RuntimeAnimatorController AnimatorController =>
        animatorController;

    public float ForwardStepSpeed =>
    forwardStepSpeed;

    public int ForwardStepFrames =>
        forwardStepFrames;

    public float BackStepSpeed =>
        backStepSpeed;

    public int BackStepFrames =>
        backStepFrames;

    public float DashSpeed =>
        dashSpeed;

}
