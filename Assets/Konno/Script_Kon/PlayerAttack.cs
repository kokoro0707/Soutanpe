using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 1つの攻撃技のデータ。Inspectorで自由に追加・調整できる。
/// </summary>
[Serializable]
public class AttackMove
{
    public string moveName = "弱パンチ";
    public Key inputKey = Key.J;

    [Header("性能")]
    public int damage = 5;
    public float range = 1.0f;
    public float knockback = 3f;

    [Header("タイミング(秒)")]
    [Tooltip("キーを押してから実際にヒット判定が出るまでの時間(発生の速さ)")]
    public float startup = 0.05f;
    [Tooltip("攻撃後、次の入力を受け付けるまでの硬直時間")]
    public float recovery = 0.15f;

    [HideInInspector] public float cooldownTimer;
}

/// <summary>
/// デバッグ用の仮Player攻撃コントローラ。
/// 弱P/強P/弱K/強K の4種類の攻撃を持ち、それぞれ
/// ダメージ・射程・発生の速さ・硬直・ノックバックを個別に設定できる。
/// PlayerController(移動)とは別コンポーネントとして分離。
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class PlayerAttack : MonoBehaviour
{
    [Header("攻撃モーション一覧")]
    [SerializeField]
    private List<AttackMove> attacks = new List<AttackMove>
    {
        new AttackMove { moveName = "弱パンチ", inputKey = Key.J, damage = 4,  range = 1.0f, knockback = 2f, startup = 0.05f, recovery = 0.10f },
        new AttackMove { moveName = "強パンチ", inputKey = Key.K, damage = 10, range = 1.1f, knockback = 5f, startup = 0.20f, recovery = 0.30f },
        new AttackMove { moveName = "弱キック", inputKey = Key.U, damage = 5,  range = 1.2f, knockback = 3f, startup = 0.08f, recovery = 0.15f },
        new AttackMove { moveName = "強キック", inputKey = Key.I, damage = 13, range = 1.3f, knockback = 7f, startup = 0.28f, recovery = 0.35f },
    };

    [Header("参照")]
    [SerializeField] private LayerMask enemyLayer;

    /// <summary>攻撃がヒットした時に発火 (技名, ダメージ)</summary>
    public event Action<string, int> OnAttackHit;

    private HealthSystem health;
    private bool isAttacking;

    private void Awake()
    {
        health = GetComponent<HealthSystem>();
    }

    private void Update()
    {
        TickCooldowns();

        if (health.IsDead || isAttacking) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        foreach (var move in attacks)
        {
            if (move.cooldownTimer <= 0f && kb[move.inputKey].wasPressedThisFrame)
            {
                StartCoroutine(DoAttack(move));
                break; // 1フレームにつき1技のみ受け付け
            }
        }
    }

    private void TickCooldowns()
    {
        foreach (var move in attacks)
        {
            if (move.cooldownTimer > 0f) move.cooldownTimer -= Time.deltaTime;
        }
    }

    private float GetFacingDir() => transform.localScale.x >= 0 ? 1f : -1f;

    private IEnumerator DoAttack(AttackMove move)
    {
        isAttacking = true;
        move.cooldownTimer = move.startup + move.recovery;

        if (move.startup > 0f)
            yield return new WaitForSeconds(move.startup);

        float facingDir = GetFacingDir();
        Vector2 origin = (Vector2)transform.position + new Vector2(facingDir * 0.5f, 0);
        Collider2D hit = Physics2D.OverlapCircle(origin, move.range, enemyLayer);

        if (hit != null)
        {
            if (hit.TryGetComponent(out HealthSystem enemyHealth))
            {
                enemyHealth.TakeDamage(move.damage);
                OnAttackHit?.Invoke(move.moveName, move.damage);
            }

            if (hit.TryGetComponent(out Rigidbody2D enemyRb))
            {
                enemyRb.AddForce(new Vector2(facingDir * move.knockback, 1f), ForceMode2D.Impulse);
            }

            Debug.Log($"[PlayerAttack] {move.moveName} ヒット! ダメージ:{move.damage}");
        }

        if (move.recovery > 0f)
            yield return new WaitForSeconds(move.recovery);

        isAttacking = false;
    }

    // 各技の攻撃範囲をSceneビューで確認するためのGizmo
    private void OnDrawGizmosSelected()
    {
        if (attacks == null) return;

        float facingDir = Application.isPlaying ? GetFacingDir() : 1f;
        Gizmos.color = Color.cyan;
        foreach (var move in attacks)
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(facingDir * 0.5f, 0);
            Gizmos.DrawWireSphere(origin, move.range);
        }
    }
}