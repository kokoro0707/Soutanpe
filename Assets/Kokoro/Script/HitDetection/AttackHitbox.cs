using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UŒ‚‚·‚é‘¤‚Ì“–‚½‚è”»’è‚ğŠÇ—‚·‚éB
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class AttackHitbox : MonoBehaviour
{
    private BoxCollider2D hitboxCollider;

    private FighterHealth ownerHealth;
    private MoveData currentMove;

    private int currentAttackDirection = 1;

    // “¯‚¶‹Z‚Å“¯‚¶‘Šè‚Ö•¡”‰ñ“–‚½‚é‚Ì‚ğ–h‚®
    private readonly HashSet<FighterHitReceiver>
        hitTargets =
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
    }

    /// <summary>
    /// UŒ‚”»’è‚ğ—LŒø‰»‚·‚éB
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

        currentAttackDirection =
            facingDirection >= 0 ? 1 : -1;

        hitTargets.Clear();

        Vector2 offset =
            move.HitboxOffset;

        offset.x =
            Mathf.Abs(offset.x) *
            currentAttackDirection;

        hitboxCollider.offset = offset;
        hitboxCollider.size =
            move.HitboxSize;

        hitboxCollider.enabled = true;
    }

    public void Deactivate()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        currentMove = null;
        ownerHealth = null;
        currentAttackDirection = 1;

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

        FighterHitReceiver targetReceiver =
            hurtbox.OwnerReceiver;

        if (targetReceiver == null)
        {
            return;
        }

        FighterHealth targetHealth =
            targetReceiver.OwnerHealth;

        if (targetHealth == null ||
            targetHealth == ownerHealth)
        {
            return;
        }

        if (!hitTargets.Add(targetReceiver))
        {
            return;
        }

        Transform attackerTransform =
            ownerHealth != null
                ? ownerHealth.transform
                : transform.root;

        targetReceiver.ReceiveAttack(
            currentMove,
            currentAttackDirection,
            attackerTransform
        );

        string attackerName =
            ownerHealth != null
                ? ownerHealth.name
                : name;

        Debug.Log(
            $"{attackerName}‚Ì" +
            $"{currentMove.MoveName}‚ª" +
            $"{targetHealth.name}‚ÉÚG",
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
