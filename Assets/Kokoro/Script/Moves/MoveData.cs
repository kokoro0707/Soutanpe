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

    [Tooltip("攻撃判定の大きさ")]
    [SerializeField]
    private Vector2 hitboxSize =
        new Vector2(1.2f, 1f);


    [Header("アニメーション")]
    [Tooltip("この技に対応するAnimator上の番号")]
    [SerializeField, Min(0)]
    private int animationIndex;


    [Header("SP")]
    [SerializeField, Min(0)]
    private int spCost = 0;


    [Header("技エフェクト")]
    [SerializeField]
    private GameObject effectPrefab;

    [SerializeField]
    private Vector2 effectOffset = Vector2.zero;


    [Header("攻撃中の打ち上げ移動")]
    [SerializeField]
    private bool useLaunch;

    [SerializeField, Min(0f)]
    private float launchVelocityY = 8f;

    [SerializeField, Min(0)]
    private int launchFrame = 3;


    [Header("攻撃中の移動")]
    [SerializeField]
    private bool useMove;

    [SerializeField]
    private float moveSpeed = 15f;

    [SerializeField, Min(0)]
    private int moveStartFrame = 0;

    [SerializeField, Min(0)]
    private int moveEndFrame = 0;


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

    public int AnimationIndex => animationIndex;

    public int SPCost => spCost;

    public GameObject EffectPrefab => effectPrefab;
    public Vector2 EffectOffset => effectOffset;

    public bool UseLaunch => useLaunch;
    public float LaunchVelocityY => launchVelocityY;
    public int LaunchFrame => launchFrame;

    public bool UseMove => useMove;
    public float MoveSpeed => moveSpeed;


    public int TotalFrames =>
        startupFrames +
        activeFrames +
        recoveryFrames;


    /// <summary>
    /// 攻撃中に移動するフレームか。
    /// </summary>
    public bool IsMoveFrame(int frame)
    {
        if (!useMove)
        {
            return false;
        }

        return frame >= moveStartFrame &&
               frame <= moveEndFrame;
    }


    /// <summary>
    /// 現在フレームが攻撃判定の持続中か。
    /// </summary>
    public bool IsActiveFrame(int currentFrame)
    {
        int activeStart =
            startupFrames;

        int activeEnd =
            startupFrames +
            activeFrames;

        return currentFrame >= activeStart &&
               currentFrame < activeEnd;
    }


    /// <summary>
    /// 現在フレームがリカバリー中か。
    /// </summary>
    public bool IsRecoveryFrame(int frame)
    {
        int recoveryStart =
            startupFrames +
            activeFrames;

        return frame >= recoveryStart &&
               frame < TotalFrames;
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
