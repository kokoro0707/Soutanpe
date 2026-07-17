using UnityEngine;

public class Fighter : MonoBehaviour
{
    public float maxHP = 420;
    public float currentHP;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void Damage(float damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }

    public void Heal(float value)
    {
        currentHP += value;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }
}