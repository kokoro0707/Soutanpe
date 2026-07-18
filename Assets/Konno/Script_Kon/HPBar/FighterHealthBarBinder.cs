using UnityEngine;

/// <summary>
/// FighterHealthのHP変化イベントをHPBarに橋渡しする。
/// PlayerにもEnemyにも同じスクリプトをそのまま使える。
/// </summary>
public sealed class FighterHealthBarBinder : MonoBehaviour
{
    [SerializeField] private FighterHealth health;
    [SerializeField] private HPBar hpBar;

    private void Reset()
    {
        health = GetComponent<FighterHealth>();
    }

    private void OnEnable()
    {
        if (health == null || hpBar == null)
        {
            Debug.LogWarning($"[FighterHealthBarBinder] {name}: healthまたはhpBarが未設定です。 health={(health != null)} hpBar={(hpBar != null)}", this);
            return;
        }

        health.OnHealthChanged += HandleHealthChanged;
    }

    private void Start()
    {
        if (health == null || hpBar == null) return;

        // Startはシーン内の全Awakeが終わった後に呼ばれることが保証されているため、
        // ここで初回反映すればFighterHealth.Awake()の実行順に関係なく正しい値になる
        Debug.Log($"[FighterHealthBarBinder] {name} Start初回反映 current={health.CurrentHP} max={health.MaxHP}", this);
        HandleHealthChanged(health.CurrentHP, health.MaxHP);
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        Debug.Log($"[FighterHealthBarBinder] {name} HandleHealthChanged current={current} max={max} hpBar={(hpBar != null ? hpBar.name : "null")}", this);
        hpBar.SetHealth(current, max);
    }
}