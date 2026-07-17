using UnityEngine;

/// <summary>
/// 技1つ分の設定データ。
/// ダメージやフレーム、攻撃判定をInspectorから調整する。
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

    [Header("フレーム")]
    [Tooltip("攻撃判定が出るまでのフレーム数")]
    [SerializeField, Min(0)]
    private int startupFrames = 5;

    [Tooltip("攻撃判定が出ているフレーム数")]
    [SerializeField, Min(1)]
    private int activeFrames = 3;

    [Tooltip("攻撃判定が消えた後の硬直フレーム数")]
    [SerializeField, Min(0)]
    private int recoveryFrames = 10;

    [Header("攻撃性能")]
    [SerializeField, Min(0)]
    private int damage = 100;

    [Header("攻撃判定")]
    [Tooltip("キャラクター中心からの攻撃判定位置")]
    [SerializeField]
    private Vector2 hitboxOffset =
        new Vector2(1f, 0f);

    [Tooltip("攻撃判定の大きさ")]
    [SerializeField]
    private Vector2 hitboxSize =
        new Vector2(1f, 1f);

    public string MoveName => moveName;

    public int StartupFrames => startupFrames;
    public int ActiveFrames => activeFrames;
    public int RecoveryFrames => recoveryFrames;

    public int Damage => damage;

    public Vector2 HitboxOffset => hitboxOffset;
    public Vector2 HitboxSize => hitboxSize;

    public int TotalFrames =>
        startupFrames +
        activeFrames +
        recoveryFrames;

    /// <summary>
    /// 現在フレームが攻撃判定の持続中か調べる。
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

        hitboxSize.x =
            Mathf.Max(0.01f, hitboxSize.x);

        hitboxSize.y =
            Mathf.Max(0.01f, hitboxSize.y);
    }
}
