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

    // ƒRƒ“ƒ{‚È‚Ç‚É‚æ‚éƒ_ƒ[ƒW•â³
    private float currentDamageMultiplier = 1f;

    // “¯‚¶‹Z‚Å“¯‚¶‘Šè‚Ö•¡”‰ñ“–‚½‚é‚Ì‚ğ–h‚®
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
    }


    /// <summary>
    /// UŒ‚”»’è‚ğ—LŒø‰»‚·‚éB
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

        // ¡‰ñ‚ÌUŒ‚‚Ìƒ_ƒ[ƒW•â³
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
        // ”»’èˆÊ’u
        // =========================

        Vector2 offset =
            move.HitboxOffset;

        offset.x =
            Mathf.Abs(offset.x) *
            currentAttackDirection;

        hitboxCollider.offset =
            offset;

        hitboxCollider.size =
            move.HitboxSize;

        hitboxCollider.enabled =
            true;
    }


    /// <summary>
    /// UŒ‚”»’è‚ğ–³Œø‰»‚·‚éB
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
    /// Hurtbox‚ÉG‚ê‚½‚ÌUŒ‚ˆ—B
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

        // ©•ª©g‚É‚Í“–‚½‚ç‚È‚¢
        if (targetHealth == null ||
            targetHealth == ownerHealth)
        {
            return;
        }


        // “¯‚¶UŒ‚‚Å“¯‚¶‘Šè‚Ö
        // •¡”‰ñƒqƒbƒg‚·‚é‚Ì‚ğ–h‚®
        if (!hitTargets.Add(targetReceiver))
        {
            return;
        }


        Transform attackerTransform =
            ownerHealth != null
                ? ownerHealth.transform
                : transform.root;


        // =========================
        // ‘Šè‚ÖUŒ‚‚ğ‘—‚é
        // =========================

        targetReceiver.ReceiveAttack(
            currentMove,
            currentAttackDirection,
            attackerTransform,
            currentDamageMultiplier
        );


        string attackerName =
            ownerHealth != null
                ? ownerHealth.name
                : name;

        Debug.Log(
            $"{attackerName}‚Ì" +
            $"{currentMove.MoveName}‚ª" +
            $"{targetHealth.name}‚ÉÚG " +
            $"•â³={currentDamageMultiplier:P0}",
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

        currentAttackDirection = 1;
        currentDamageMultiplier = 1f;

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

        Gizmos.color =
            Color.red;

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
