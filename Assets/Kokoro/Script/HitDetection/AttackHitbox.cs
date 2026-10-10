using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃する側の当たり判定を管理する。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class AttackHitbox : MonoBehaviour
{
    [Header("SP")]
    [SerializeField]
    private FighterSPGauge ownerSPGauge;


    private BoxCollider2D hitboxCollider;

    private FighterHealth ownerHealth;

    private MoveData currentMove;

    private int currentAttackDirection = 1;

    // コンボなどによるダメージ補正
    private float currentDamageMultiplier = 1f;


    // 同じ技で同じ相手へ複数回当たるのを防ぐ
    private readonly HashSet<FighterHitReceiver> hitTargets =
        new HashSet<FighterHitReceiver>();


    public bool IsActive =>
        hitboxCollider != null &&
        hitboxCollider.enabled;


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
    /// 攻撃判定を有効化する。
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


        hitTargets.Clear();


        // =========================
        // 判定位置
        // =========================

        Vector2 offset =
            move.HitboxOffset;


        offset.x =
            Mathf.Abs(offset.x) *
            currentAttackDirection;


        hitboxCollider.offset =
            offset;


        // =========================
        // 判定サイズ
        // =========================

        hitboxCollider.size =
            move.HitboxSize;


        hitboxCollider.enabled =
            true;
    }


    /// <summary>
    /// 攻撃判定を無効化する。
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
    /// Hurtboxに触れた時の攻撃処理。
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


        // 同じ攻撃で同じ相手へ
        // 複数回ヒットするのを防ぐ
        if (!hitTargets.Add(targetReceiver))
        {
            return;
        }


        Transform attackerTransform =
            ownerHealth != null
                ? ownerHealth.transform
                : transform.root;


        // =========================
        // 相手へ攻撃を送る
        // =========================

        targetReceiver.ReceiveAttack(
            currentMove,
            currentAttackDirection,
            attackerTransform,
            currentDamageMultiplier
        );


        // =========================
        // 攻撃が当たったのでSP回復
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
    /// 攻撃中の判定をSceneビューに表示。
    /// </summary>
    private void OnDrawGizmos()
    {
        BoxCollider2D box =
            GetComponent<BoxCollider2D>();


        if (box == null ||
            !Application.isPlaying ||
            !box.enabled)
        {
            return;
        }


        Gizmos.color =
            UnityEngine.Color.red;


        Matrix4x4 previousMatrix =
            Gizmos.matrix;


        Gizmos.matrix =
            transform.localToWorldMatrix;


        Gizmos.DrawWireCube(
            box.offset,
            box.size
        );

        Gizmos.matrix =
            previousMatrix;
    }
}
 