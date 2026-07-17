using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class DePlayerController : MonoBehaviour
{
    [Header("移動")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;

    [Header("接地判定")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("攻撃")]
    [SerializeField] private GameObject attackHitbox;
    [SerializeField] private float attackTime = 0.2f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool attacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (attackHitbox != null)
            attackHitbox.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // 左右移動
        float move = 0;

        if (keyboard.aKey.isPressed)
            move = -1;

        if (keyboard.dKey.isPressed)
            move = 1;

        rb.linearVelocity = new Vector2(move * moveSpeed, rb.linearVelocity.y);

        // 接地判定
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer);

        // ジャンプ
        if (keyboard.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // デバッグ攻撃(Jキー)
        if (keyboard.jKey.wasPressedThisFrame && !attacking)
        {
            StartCoroutine(Attack());
        }
    }

    private IEnumerator Attack()
    {
        attacking = true;

        attackHitbox.SetActive(true);

        yield return new WaitForSeconds(attackTime);

        attackHitbox.SetActive(false);

        attacking = false;
    }

    // 接地判定をSceneビューで表示
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}