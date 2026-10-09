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
                FighterState.KnockDown
            );
        }

        OnKnockedOut?.Invoke();
    }

    /// <summary>
    /// HPを満タンに戻し、KOで止まっていたステートマシンも通常状態(Idle)に戻す。
    /// ラウンドが切り替わる際、MatchScoreManagerのOn Round Continueイベントから
    /// P1・P2それぞれのこのメソッドを呼び出す想定。
    /// </summary>
    public void ResetHealth()
    {
        CurrentHP = maxHP;

        OnHealthChanged?.Invoke(
            CurrentHP,
            maxHP
        );

        // KO時にForceChangeState(FighterState.KO)で止めたステートマシンを、
        // 次のラウンドのために通常状態へ戻す。
        // (TakeDamage側でKOにした時と対になる処理)
        if (stateMachine != null)
        {
            stateMachine.ForceChangeState(
                FighterState.Idle
            );
        }
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