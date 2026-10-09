using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

/// <summary>
/// 攻撃する側の当たり判定を管理する。
/// 1つのBoxCollider2Dを技ごとに位置・サイズ変更して使用する。
/// </summary>
/// [ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class AttackHitbox : MonoBehaviour


{
    [Header("SP")]
    [SerializeField]
    private FighterSPGauge ownerSPGauge;


    [Header("Sceneビュー確認用")]
    [Tooltip("Sceneビューで確認したいMoveData")]
    [SerializeField]
    private MoveData previewMove;

    [SerializeField]
    private bool showHitboxPreview = true;

    [Tooltip("1 = 右向き / -1 = 左向き")]
    [SerializeField]
    private int previewFacingDirection = 1;


    private BoxCollider2D hitboxCollider;

    private FighterHealth ownerHealth;

    private MoveData currentMove;

    private int currentAttackDirection = 1;

    // コンボなどによるダメージ補正
    private float currentDamageMultiplier = 1f;


    // 同じ技で同じ相手へ複数回当たるのを防ぐ
    private readonly HashSet<FighterHitReceiver> hitTargets =
        new HashSet<FighterHitReceiver>();


    /// <summary>
    /// 現在、攻撃判定が有効か。
    /// </summary>
    public bool IsActive =>
        hitboxCollider != null &&
        hitboxCollider.enabled;



    private void Update()
    {
        // ゲーム中は通常処理に任せる
        if (Application.isPlaying)
        {
            return;
        }

        // プレビューする技が無ければ何もしない
        if (previewMove == null)
        {
            return;
        }

        // Collider取得
        if (hitboxCollider == null)
        {
            hitboxCollider =
                GetComponent<BoxCollider2D>();
        }

        if (hitboxCollider == null)
        {
            return;
        }


        // =========================
        // MoveDataのOffsetを反映
        // =========================

        Vector2 offset =
            previewMove.HitboxOffset;

        int direction =
            previewFacingDirection >= 0
                ? 1
                : -1;

        offset.x =
            Mathf.Abs(offset.x) *
            direction;

        hitboxCollider.offset =
            offset;


        // =========================
        // MoveDataのSizeを反映
        // =========================

        hitboxCollider.size =
            previewMove.HitboxSize;

        hitboxCollider.isTrigger =
            true;
    }

    private void Awake()
    {
        hitboxCollider =
            GetComponent<BoxCollider2D>();

        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;


        if (ownerSPGauge == null)
        {
            ownerSPGauge =
                GetComponentInParent<FighterSPGauge>();
        }
    }


    /// <summary>
    /// 攻撃判定を有効にする。
    /// </summary>
    public void Activate(
        MoveData move,
        int facingDirection,
        FighterHealth attackOwner,
        float damageMultiplier = 1f
    )
    {
        if (move == null ||
            hitboxCollider == null)
        {
            return;
        }


        currentMove = move;

        ownerHealth = attackOwner;


        currentDamageMultiplier =
            Mathf.Max(
                0f,
                damageMultiplier
            );


        currentAttackDirection =
            facingDirection >= 0
                ? 1
                : -1;


        // 新しい攻撃なので
        // ヒット済みリストをリセット
        hitTargets.Clear();


        // =========================
        // 攻撃判定の位置
        // =========================

        Vector2 offset =
            move.HitboxOffset;


        // 左向きならXだけ反転
        offset.x =
            Mathf.Abs(offset.x) *
            currentAttackDirection;


        hitboxCollider.offset =
            offset;


        // =========================
        // 攻撃判定の大きさ
        // =========================

        hitboxCollider.size =
            move.HitboxSize;


        hitboxCollider.enabled =
            true;
    }


    /// <summary>
    /// 攻撃判定を無効にする。
    /// </summary>
    public void Deactivate()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }


        currentMove = null;

        ownerHealth = null;

        currentAttackDirection = 1;

        currentDamageMultiplier = 1f;

        hitTargets.Clear();
    }


    /// <summary>
    /// Hurtboxに触れた時の処理。
    /// </summary>
    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (!IsActive ||
            currentMove == null)
        {
            return;
        }


        Hurtbox hurtbox =
            other.GetComponent<Hurtbox>();


        if (hurtbox == null)
        {
            return;
        }


        FighterHitReceiver targetReceiver =
            hurtbox.OwnerReceiver;


        if (targetReceiver == null)
        {
            return;
        }


        FighterHealth targetHealth =
            targetReceiver.OwnerHealth;


        // 自分自身には当たらない
        if (targetHealth == null ||
            targetHealth == ownerHealth)
        {
            return;
        }


        // 同じ攻撃で同じ相手に
        // 複数回ヒットしない
        if (!hitTargets.Add(targetReceiver))
        {
            return;
        }


        Transform attackerTransform =
            ownerHealth != null
                ? ownerHealth.transform
                : transform.root;


        // =========================
        // 相手へ攻撃
        // =========================

        targetReceiver.ReceiveAttack(
            currentMove,
            currentAttackDirection,
            attackerTransform,
            currentDamageMultiplier
        );


        // =========================
        // ヒット時SP回復
        // =========================

        if (ownerSPGauge != null)
        {
            ownerSPGauge.AddSPOnHit();
        }
    }


    private void OnDisable()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }


        currentMove = null;

        ownerHealth = null;

        currentAttackDirection = 1;

        currentDamageMultiplier = 1f;

        hitTargets.Clear();
    }


    /// <summary>
    /// Sceneビューで攻撃判定を表示する。
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showHitboxPreview)
        {
            return;
        }


        // =========================
        // Play中
        // 実際のColliderを表示
        // =========================

        if (Application.isPlaying)
        {
            BoxCollider2D box =
                GetComponent<BoxCollider2D>();


            if (box == null ||
                !box.enabled)
            {
                return;
            }


            Gizmos.color =
                UnityEngine.Color.red;


            Matrix4x4 oldMatrix =
                Gizmos.matrix;


            Gizmos.matrix =
                transform.localToWorldMatrix;


            Gizmos.DrawWireCube(
                box.offset,
                box.size
            );


            Gizmos.matrix =
                oldMatrix;


            return;
        }


        // =========================
        // Edit中
        // Preview Moveを表示
        // =========================

        if (previewMove == null)
        {
            return;
        }


        Vector2 offset =
            previewMove.HitboxOffset;


        int direction =
            previewFacingDirection >= 0
                ? 1
                : -1;


        offset.x =
            Mathf.Abs(offset.x) *
            direction;


        Gizmos.color =
            UnityEngine.Color.red;


        Matrix4x4 previousMatrix =
            Gizmos.matrix;


        Gizmos.matrix =
            transform.localToWorldMatrix;


        Gizmos.DrawWireCube(
            offset,
            previewMove.HitboxSize
        );


        Gizmos.matrix =
            previousMatrix;
    }
}
 