using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DeEnemyController : MonoBehaviour
{
    public Transform Deplayer;

    public float moveSpeed = 3f;

    public float stopDistance = 1.5f;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        float distance = Vector2.Distance(
            transform.position,
            Deplayer.position);

        if (distance > stopDistance)
        {
            float dir = Mathf.Sign(Deplayer.position.x - transform.position.x);

            rb.linearVelocity = new Vector2(
                dir * moveSpeed,
                rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(
                0,
                rb.linearVelocity.y);
        }
    }
}