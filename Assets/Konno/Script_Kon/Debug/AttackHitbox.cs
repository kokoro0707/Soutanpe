using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private Fighter owner;
    [SerializeField] private float damage = 30f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Fighter target = other.GetComponent<Fighter>();

        if (target == null)
            return;

        if (target == owner)
            return;

        target.Damage(damage);
    }
    private void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();

        if (col == null) return;

        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(col.offset, col.size);
    }
}