using UnityEngine;

/// <summary>
/// 攻撃を受ける側の判定。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class Hurtbox : MonoBehaviour
{
    [Header("所有者")]
    [SerializeField]
    private FighterHitReceiver ownerReceiver;

    public FighterHitReceiver OwnerReceiver =>
        ownerReceiver;

    private void Reset()
    {
        ownerReceiver =
            GetComponentInParent<FighterHitReceiver>();

        Collider2D hurtboxCollider =
            GetComponent<Collider2D>();

        if (hurtboxCollider != null)
        {
            hurtboxCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (ownerReceiver == null)
        {
            ownerReceiver =
                GetComponentInParent<FighterHitReceiver>();
        }

        if (ownerReceiver == null)
        {
            Debug.LogError(
                $"{name}の親に" +
                "FighterHitReceiverがありません。",
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
