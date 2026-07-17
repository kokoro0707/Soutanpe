using UnityEngine;

public enum FighterState
{
    Idle,
    Walk,
    Jump,

    Guard,
    ForwardStep,
    BackStep,
    Dash,

    Attack,
    HitStun,
    BlockStun,
    KnockDown,
    KO
}

/// <summary>
/// キャラクターの現在状態を管理する。
/// </summary>
public sealed class FighterStateMachine : MonoBehaviour
{
    [SerializeField]
    private FighterState initialState =
        FighterState.Idle;

    public FighterState CurrentState
    {
        get;
        private set;
    }

    /// <summary>
    /// 歩行、ジャンプ、ステップを新しく開始できる状態。
    /// </summary>
    public bool CanStartMovement
    {
        get
        {
            return CurrentState == FighterState.Idle ||
                   CurrentState == FighterState.Walk ||
                   CurrentState == FighterState.Guard;
        }
    }

    /// <summary>
    /// 相手の方向へ自動で振り向ける状態。
    /// </summary>
    public bool CanAutoTurn
    {
        get
        {
            return CurrentState == FighterState.Idle ||
                   CurrentState == FighterState.Walk ||
                   CurrentState == FighterState.Guard;
        }
    }

    /// <summary>
    /// 攻撃や被弾など、移動状態で上書きしてはいけない状態。
    /// </summary>
    public bool IsCombatLocked
    {
        get
        {
            return CurrentState == FighterState.Attack ||
                   CurrentState == FighterState.HitStun ||
                   CurrentState == FighterState.BlockStun ||
                   CurrentState == FighterState.KnockDown ||
                   CurrentState == FighterState.KO;
        }
    }

    private void Awake()
    {
        CurrentState = initialState;
    }

    public bool TryChangeState(
        FighterState nextState
    )
    {
        if (CurrentState == FighterState.KO)
        {
            return false;
        }

        CurrentState = nextState;
        return true;
    }

    public void ForceChangeState(
        FighterState nextState
    )
    {
        CurrentState = nextState;
    }
}
