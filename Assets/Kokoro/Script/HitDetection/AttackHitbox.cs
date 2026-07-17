using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃する側の当たり判定を管理する。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class AttackHitbox : MonoBehaviour
{
    private BoxCollider2D hitboxCollider;

    private FighterHealth ownerHealth;
    private MoveData currentMove;

    // 1回の技で同じ相手に複数回当たるのを防ぐ
    private readonly HashSet<FighterHealth> hitTargets =
        new HashSet<FighterHealth>();

    /// <summary>
    /// 現在、攻撃判定が有効か。
    /// </summary>
    public bool IsActive =>
        hitboxCollider != null &&
        hitboxCollider.enabled;

    private void Awake()
    {
        hitboxCollider =
            GetComponent<BoxCollider2D>();

        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
    }

    /// <summary>
    /// 攻撃判定を有効化する。
    /// キャラクターの向きに合わせてX座標を反転する。
    /// </summary>
    public void Activate(
        MoveData move,
        int facingDirection,
        FighterHealth attackOwner
    )
    {
        if (move == null ||
            hitboxCollider == null)
        {
            return;
        }

        currentMove = move;
        ownerHealth = attackOwner;

        hitTargets.Clear();

        // 右向きなら1、左向きなら-1
        int direction =
            facingDirection >= 0 ? 1 : -1;

        Vector2 offset =
            move.HitboxOffset;

        // 攻撃判定の横位置だけ反転する
        offset.x =
            Mathf.Abs(offset.x) * direction;

        hitboxCollider.offset = offset;
        hitboxCollider.size = move.HitboxSize;
        hitboxCollider.enabled = true;

        string ownerName =
            ownerHealth != null
                ? ownerHealth.name
                : name;

        Debug.Log(
            $"{ownerName} 攻撃方向：{direction} " +
            $"Hitbox X：{offset.x}",
            this
        );
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

        hitTargets.Clear();
    }

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

        FighterHealth targetHealth =
            hurtbox.OwnerHealth;

        if (targetHealth == null)
        {
            return;
        }

        // 自分自身には当てない
        if (targetHealth == ownerHealth)
        {
            return;
        }

        // 同じ攻撃で同じ相手に複数回当てない
        if (!hitTargets.Add(targetHealth))
        {
            return;
        }

        targetHealth.TakeDamage(
            currentMove.Damage
        );

        Debug.Log(
            $"{ownerHealth.name}の" +
            $"{currentMove.MoveName}が" +
            $"{targetHealth.name}にヒット",
            this
        );
    }

    private void OnDisable()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        currentMove = null;
        ownerHealth = null;

        hitTargets.Clear();
    }

    /// <summary>
    /// 攻撃判定が有効な間だけ赤枠を表示する。
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

        Gizmos.color = Color.red;

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
