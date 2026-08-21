using UnityEngine;

/// <summary>
/// キャラクターの通常投げ性能。
/// </summary>
[CreateAssetMenu(
    fileName = "Grab_",
    menuName = "Fighting Game/Grab Data"
)]
public sealed class GrabData : ScriptableObject
{
    [Header("つかみ判定")]
    [SerializeField, Min(0)]
    private int startupFrames = 5;

    [SerializeField, Min(1)]
    private int activeFrames = 3;

    [SerializeField, Min(0)]
    private int whiffRecoveryFrames = 15;

    [Header("成功時")]
    [SerializeField, Min(1)]
    private int holdFrames = 12;

    [SerializeField, Min(0)]
    private int throwRecoveryFrames = 15;

    [Header("投げ性能")]
    [SerializeField, Min(0)]
    private int throwDamage = 120;

    [SerializeField, Min(1)]
    private int throwHitStunFrames = 20;

    [SerializeField]
    private Vector2 throwKnockback =
        new Vector2(7f, 3f);

    [Header("つかみ判定サイズ")]
    [SerializeField]
    private Vector2 hitboxOffset =
        new Vector2(0.8f, 0f);

    [SerializeField]
    private Vector2 hitboxSize =
        new Vector2(1f, 1.5f);

    [Header("つかんだ相手の位置")]
    [SerializeField]
    private Vector2 holdOffset =
        new Vector2(0.65f, 0f);

    public int StartupFrames => startupFrames;
    public int ActiveFrames => activeFrames;
    public int WhiffRecoveryFrames => whiffRecoveryFrames;

    public int HoldFrames => holdFrames;
    public int ThrowRecoveryFrames => throwRecoveryFrames;

    public int ThrowDamage => throwDamage;
    public int ThrowHitStunFrames => throwHitStunFrames;
    public Vector2 ThrowKnockback => throwKnockback;

    public Vector2 HitboxOffset => hitboxOffset;
    public Vector2 HitboxSize => hitboxSize;
    public Vector2 HoldOffset => holdOffset;
}
