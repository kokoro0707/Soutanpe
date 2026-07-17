using UnityEngine;

public class Fighter : MonoBehaviour
{
    public float maxHP = 420;
    public float currentHP;

    public bool IsDead => currentHP <= 0;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void Damage(float damage)
    {
        if (IsDead || damage <= 0) return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }

    public void Heal(float value)
    {
        if (IsDead || value <= 0) return;

        currentHP += value;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }
}