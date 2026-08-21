using System;
using UnityEngine;

/// <summary>
/// キャラクターのHPとKOを管理する。
/// </summary>
public sealed class FighterHealth : MonoBehaviour
{
    [Header("HP")]
    [SerializeField, Min(1)]
    private int maxHP = 1000;

    [Header("参照")]
    [SerializeField]
    private FighterStateMachine stateMachine;

    public int CurrentHP { get; private set; }

    public int MaxHP => maxHP;

    public bool IsKnockedOut =>
        CurrentHP <= 0;

    public event Action<int, int> OnHealthChanged;
    public event Action OnKnockedOut;

    private void Reset()
    {
        stateMachine =
            GetComponent<FighterStateMachine>();
    }

    private void Awake()
    {
        CurrentHP = maxHP;
    }

    /// <summary>
    /// 指定されたダメージを受ける。
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsKnockedOut)
        {
            return;
        }

        CurrentHP =
            Mathf.Max(CurrentHP - damage, 0);

        Debug.Log(
            $"{name}が{damage}ダメージを受けました。" +
            $" 残りHP：{CurrentHP}/{maxHP}",
            this
        );

        OnHealthChanged?.Invoke(
            CurrentHP,
            maxHP
        );

        if (CurrentHP > 0)
        {
            return;
        }

        if (stateMachine != null)
        {
            stateMachine.ForceChangeState(
                FighterState.KO
            );
        }

        Debug.Log($"{name} KO", this);

        OnKnockedOut?.Invoke();
    }

    public void ResetHealth()
    {
        CurrentHP = maxHP;

        OnHealthChanged?.Invoke(
            CurrentHP,
            maxHP
        );
    }

    /// <summary>
    /// キャラクターデータから最大HPを設定する。
    /// </summary>
    public void SetMaxHP(
        int newMaxHP,
        bool refill = true
    )
    {
        maxHP =
            Mathf.Max(1, newMaxHP);

        if (refill)
        {
            CurrentHP = maxHP;
        }
        else
        {
            CurrentHP =
                Mathf.Min(
                    CurrentHP,
                    maxHP
                );
        }

        OnHealthChanged?.Invoke(
            CurrentHP,
            maxHP
        );
    }

}
