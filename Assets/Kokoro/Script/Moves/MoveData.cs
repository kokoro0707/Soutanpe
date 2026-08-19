using UnityEngine;

/// <summary>
/// 技1つ分の設定データ。
/// ダメージ、フレーム、攻撃判定、ヒット効果を管理する。
/// </summary>
[CreateAssetMenu(
    fileName = "Move_",
    menuName = "Fighting Game/Move Data"
)]
public sealed class MoveData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField]
    private string moveName = "弱攻撃";

    [Header("攻撃フレーム")]
    [Tooltip("入力から攻撃判定が出るまで")]
    [SerializeField, Min(0)]
    private int startupFrames = 5;

    [Tooltip("攻撃判定が出ている時間")]
    [SerializeField, Min(1)]
    private int activeFrames = 3;

    [Tooltip("攻撃判定が消えた後の硬直")]
    [SerializeField, Min(0)]
    private int recoveryFrames = 10;

    [Header("攻撃性能")]
    [SerializeField, Min(0)]
    private int damage = 100;

    [Tooltip("攻撃が当たった相手の行動不能時間")]
    [SerializeField, Min(1)]
    private int hitStunFrames = 18;

    [Tooltip("ガードした相手の行動不能時間")]
    [SerializeField, Min(1)]
    private int blockStunFrames = 10;

    [Header("ヒット時ノックバック")]
    [Tooltip("Xは攻撃方向へ飛ばす量")]
    [SerializeField]
    private Vector2 hitKnockback =
        new Vector2(6f, 2f);

    [Header("ガード時ノックバック")]
    [SerializeField]
    private Vector2 blockKnockback =
        new Vector2(3f, 0f);

    [Header("攻撃判定")]
    [Tooltip("右向きを基準にした攻撃判定位置")]
    [SerializeField]
    private Vector2 hitboxOffset =
        new Vector2(1f, 0f);

    [Header("アニメーション")]
    [Tooltip("この技に対応するAnimator上の番号")]
    [SerializeField, Min(0)]
    private int animationIndex;
    

    [SerializeField]
    private Vector2 hitboxSize =
        new Vector2(1.2f, 1f);

    public string MoveName => moveName;

    public int StartupFrames => startupFrames;
    public int ActiveFrames => activeFrames;
    public int RecoveryFrames => recoveryFrames;

    public int Damage => damage;
    public int HitStunFrames => hitStunFrames;
    public int BlockStunFrames => blockStunFrames;

    public Vector2 HitKnockback => hitKnockback;
    public Vector2 BlockKnockback => blockKnockback;

    public Vector2 HitboxOffset => hitboxOffset;
    public Vector2 HitboxSize => hitboxSize;

    public int TotalFrames =>
        startupFrames +
        activeFrames +
        recoveryFrames;

    public int AnimationIndex => animationIndex;

    /// <summary>
    /// 現在フレームが攻撃判定の持続中か。
    /// </summary>
    public bool IsActiveFrame(int currentFrame)
    {
        int activeStart = startupFrames;
        int activeEnd =
            startupFrames + activeFrames;

        return currentFrame >= activeStart &&
               currentFrame < activeEnd;
    }

    private void OnValidate()
    {
        startupFrames =
            Mathf.Max(0, startupFrames);

        activeFrames =
            Mathf.Max(1, activeFrames);

        recoveryFrames =
            Mathf.Max(0, recoveryFrames);

        damage =
            Mathf.Max(0, damage);

        hitStunFrames =
            Mathf.Max(1, hitStunFrames);

        blockStunFrames =
            Mathf.Max(1, blockStunFrames);

        hitKnockback.x =
            Mathf.Abs(hitKnockback.x);

        blockKnockback.x =
            Mathf.Abs(blockKnockback.x);

        hitboxSize.x =
            Mathf.Max(0.01f, hitboxSize.x);

        hitboxSize.y =
            Mathf.Max(0.01f, hitboxSize.y);
    }
}
