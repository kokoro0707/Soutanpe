using System;
using UnityEngine;

/// <summary>
/// キャラクターのSPゲージを管理する。
/// コンボリセット・必殺技などで共通使用する。
/// </summary>
public sealed class FighterSPGauge : MonoBehaviour
{
    [Header("SP")]
    [SerializeField, Min(1)]
    private int maxSP = 2;

    [SerializeField]
    private bool startFull = true;

    public int CurrentSP
    {
        get;
        private set;
    }

    public int MaxSP => maxSP;

    /// <summary>
    /// UI更新用。
    /// CurrentSP, MaxSPを渡す。
    /// </summary>
    public event Action<int, int> OnSPChanged;


    private void Awake()
    {
        CurrentSP =
            startFull
                ? maxSP
                : 0;
    }


    private void Start()
    {
        NotifySPChanged();
    }


    /// <summary>
    /// 指定SPを払えるか。
    /// </summary>
    public bool CanConsume(int amount)
    {
        amount =
            Mathf.Max(0, amount);

        return CurrentSP >= amount;
    }


    /// <summary>
    /// SPを消費する。
    /// 足りなければfalse。
    /// </summary>
    public bool TryConsume(int amount)
    {
        amount =
            Mathf.Max(0, amount);

        if (!CanConsume(amount))
        {
            return false;
        }

        CurrentSP -= amount;

        NotifySPChanged();

        return true;
    }


    /// <summary>
    /// SPを増やす。
    /// </summary>
    public void AddSP(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentSP =
            Mathf.Clamp(
                CurrentSP + amount,
                0,
                maxSP
            );

        NotifySPChanged();
    }


    public void SetSP(int value)
    {
        CurrentSP =
            Mathf.Clamp(
                value,
                0,
                maxSP
            );

        NotifySPChanged();
    }


    public void ResetSP()
    {
        CurrentSP =
            startFull
                ? maxSP
                : 0;

        NotifySPChanged();
    }


    private void NotifySPChanged()
    {
        OnSPChanged?.Invoke(
            CurrentSP,
            maxSP
        );
    }
}
