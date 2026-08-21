using UnityEngine;

/// <summary>
/// つかみ専用の判定。
/// 通常のAttackHitboxとは別物。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class GrabHitbox : MonoBehaviour
{
    private BoxCollider2D grabCollider;

    private FighterGrabController owner;

    public bool IsActive =>
        grabCollider != null &&
        grabCollider.enabled;

    private void Awake()
    {
        grabCollider =
            GetComponent<BoxCollider2D>();

        grabCollider.isTrigger = true;
        grabCollider.enabled = false;
    }

    public void Activate(
        GrabData data,
        int facingDirection,
        FighterGrabController grabOwner
    )
    {
        if (data == null)
        {
            return;
        }

        owner = grabOwner;

        int direction =
            facingDirection >= 0 ? 1 : -1;

        Vector2 offset =
            data.HitboxOffset;

        offset.x =
            Mathf.Abs(offset.x) *
            direction;

        grabCollider.offset =
            offset;

        grabCollider.size =
            data.HitboxSize;

        grabCollider.enabled =
            true;
    }

    public void Deactivate()
    {
        if (grabCollider != null)
        {
            grabCollider.enabled =
                false;
        }

        owner = null;
    }

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (!IsActive ||
            owner == null)
        {
            return;
        }

        Hurtbox hurtbox =
            other.GetComponent<Hurtbox>();

        if (hurtbox == null)
        {
            return;
        }

        FighterGrabTarget target =
            hurtbox.GetComponentInParent<
                FighterGrabTarget>();

        if (target == null)
        {
            return;
        }

        if (target.transform ==
            owner.transform)
        {
            return;
        }

        owner.TryGrab(target);
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box =
            GetComponent<BoxCollider2D>();

        if (box == null)
        {
            return;
        }

        // プレイ中は有効な時だけ表示
        if (Application.isPlaying &&
            !box.enabled)
        {
            return;
        }

        Gizmos.color = Color.yellow;

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
    }

}
