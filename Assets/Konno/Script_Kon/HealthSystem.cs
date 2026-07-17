using System;
using UnityEngine;

/// <summary>
/// HPを管理する汎用コンポーネント。Player/Enemyどちらにもアタッチして使う。
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("HP設定")]
    [Tooltip("ギルティギア風に420を初期値にしています。キャラごとに調整可能")]
    [SerializeField] private int maxHP = 420;
    [SerializeField] private int currentHP;

    [Header("ガード設定")]
    [Tooltip("ガード中に受けるダメージの割合(0.1 = 通常の10%だけ削りダメージが入る)")]
    [SerializeField, Range(0f, 1f)] private float chipDamageRatio = 0.1f;
    [Tooltip("ガード中に削りダメージだけで倒せるかどうか(GG系は基本false)")]
    [SerializeField] private bool chipCanKill = false;

    [Header("デバッグ表示")]
    [SerializeField] private bool showDebugLabel = true;
    [SerializeField] private Color labelColor = Color.white;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0;

    /// <summary>外部(PlayerController等)からガード中かどうかをセットする</summary>
    public bool IsGuarding { get; set; }

    /// <summary>HP変化時 (現在HP, 最大HP)</summary>
    public event Action<int, int> OnHealthChanged;
    /// <summary>被ダメージ時 (ダメージ量)</summary>
    public event Action<int> OnDamaged;
    /// <summary>死亡時</summary>
    public event Action OnDeath;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        bool wasGuarded = IsGuarding;
        int finalDamage = amount;

        if (wasGuarded)
        {
            // ガード中は削りダメージのみ(最低1)
            finalDamage = Mathf.Max(1, Mathf.RoundToInt(amount * chipDamageRatio));

            // 削りダメージでは倒せない設定なら、HPが1残るように制限
            if (!chipCanKill && finalDamage >= currentHP)
            {
                finalDamage = currentHP - 1;
                if (finalDamage < 0) finalDamage = 0;
            }
        }

        currentHP = Mathf.Max(0, currentHP - finalDamage);
        OnDamaged?.Invoke(finalDamage);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        string guardText = wasGuarded ? "(ガード削り)" : "";
        Debug.Log($"[HealthSystem] {gameObject.name} が {finalDamage} ダメージ{guardText}。残りHP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            OnDeath?.Invoke();
            Debug.Log($"[HealthSystem] {gameObject.name} は死亡しました。");
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void ResetHP()
    {
        currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    // キャラクターの頭上にHPをテキスト表示(デバッグ用)
    private void OnGUI()
    {
        if (!showDebugLabel || Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (screenPos.z < 0) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = labelColor;

        string text = $"{gameObject.name}\nHP: {currentHP}/{maxHP}";
        GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 20, 100, 40), text, style);
    }
}