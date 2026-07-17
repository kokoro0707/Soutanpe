using UnityEngine;

/// <summary>
/// EnemyがPlayerに実際に攻撃してダメージを与えるスクリプト。
/// デバッグキーではなく、距離判定による自動攻撃(接近したら一定間隔で攻撃)。
/// EnemyのGameObject(Fighterが付いている側)にアタッチする。
/// </summary>
public class EnemyAttacker : MonoBehaviour
{
    [Header("攻撃対象")]
    [Tooltip("未設定ならタグ Player を自動検索してFighterを取得する")]
    [SerializeField] private Fighter target;

    [Header("攻撃設定")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 1.5f;

    private Fighter self;
    private float cooldownTimer;

    private void Awake()
    {
        self = GetComponent<Fighter>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("DePlayer");
            if (player != null) target = player.GetComponent<Fighter>();
        }
    }

    private void Update()
    {
        if (target == null || target.IsDead) return;
        if (self != null && self.IsDead) return;

        cooldownTimer -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, target.transform.position);
        if (dist <= attackRange && cooldownTimer <= 0f)
        {
            target.Damage(attackDamage);
            cooldownTimer = attackCooldown;
            Debug.Log($"[EnemyAttacker] {gameObject.name} が {target.gameObject.name} に {attackDamage} ダメージ。残りHP: {target.currentHP}/{target.maxHP}");
        }
    }

    // 攻撃範囲をSceneビューで確認するためのGizmo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}