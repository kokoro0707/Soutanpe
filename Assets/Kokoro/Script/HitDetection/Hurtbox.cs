using UnityEngine;

/// <summary>
/// キャラクターが攻撃を受けるための判定。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class Hurtbox : MonoBehaviour
{
    [Header("所有者")]
    [SerializeField]
    private FighterHealth ownerHealth;

    public FighterHealth OwnerHealth => ownerHealth;

    private void Reset()
    {
        ownerHealth =
            GetComponentInParent<FighterHealth>();

        Collider2D hurtboxCollider =
            GetComponent<Collider2D>();

        if (hurtboxCollider != null)
        {
            hurtboxCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (ownerHealth == null)
        {
            ownerHealth =
                GetComponentInParent<FighterHealth>();
        }

        if (ownerHealth == null)
        {
            Debug.LogError(
                $"{name}の親にFighterHealthがありません。",
                this
            );
        }
    }
    private void OnDrawGizmos()
    {
        BoxCollider2D box =
            GetComponent<BoxCollider2D>();

        if (box == null)
        {
            return;
        }

        Gizmos.color =
            new Color(0f, 0.7f, 1f, 1f);

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
