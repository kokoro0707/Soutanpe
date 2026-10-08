using System;
using UnityEngine;

public sealed class FighterSPGauge : MonoBehaviour
{
    [Header("最大SP")]
    [SerializeField, Min(1f)]
    private float maxSP = 2f;

    [Header("開始時")]
    [SerializeField]
    private bool startFull = false;


    // =========================
    // 自動回復
    // =========================

    [Header("自動回復")]
    [SerializeField]
    private bool useAutoRecovery = true;

    [Tooltip("1秒間に回復するSP量")]
    [SerializeField, Min(0f)]
    private float autoRecoveryPerSecond = 0.05f;


    // =========================
    // 攻撃ヒット時
    // =========================

    [Header("攻撃ヒット時の回復")]
    [Tooltip("攻撃が1回当たるたびに増えるSP量")]
    [SerializeField, Min(0f)]
    private float hitRecoveryAmount = 0.15f;


    // =========================
    // 現在値
    // =========================

    public float CurrentSP { get; private set; }

    public float MaxSP => maxSP;

    public event Action<float, float> OnSPChanged;


    private void Awake()
    {
        CurrentSP =
            startFull
                ? maxSP
                : 0f;
    }


    private void Start()
    {
        NotifySPChanged();
    }


    private void Update()
    {
        if (!useAutoRecovery)
        {
            return;
        }

        if (CurrentSP >= maxSP)
        {
            return;
        }

        AddSP(
            autoRecoveryPerSecond *
            Time.deltaTime
        );
    }


    // =========================
    // SPを使用できるか
    // =========================

    public bool CanConsume(float amount)
    {
        return CurrentSP >= amount;
    }


    // =========================
    // SP消費
    // =========================

    public bool TryConsume(float amount)
    {
        if (!CanConsume(amount))
        {
            return false;
        }

        CurrentSP -= amount;

        CurrentSP =
            Mathf.Clamp(
                CurrentSP,
                0f,
                maxSP
            );

        NotifySPChanged();

        return true;
    }


    // =========================
    // SP追加
    // =========================

    public void AddSP(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        float oldSP = CurrentSP;

        CurrentSP =
            Mathf.Clamp(
                CurrentSP + amount,
                0f,
                maxSP
            );

        // 値が変わった時だけUI更新
        if (!Mathf.Approximately(
                oldSP,
                CurrentSP
            ))
        {
            NotifySPChanged();
        }
    }


    // =========================
    // 攻撃ヒット時
    // =========================

    public void AddSPOnHit()
    {
        AddSP(hitRecoveryAmount);
    }


    // =========================
    // リセット
    // =========================

    public void ResetSP()
    {
        CurrentSP =
            startFull
                ? maxSP
                : 0f;

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
