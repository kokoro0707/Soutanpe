using UnityEngine;

/// <summary>
/// キャラクターが現在どの状態かを表す。
/// </summary>
public enum FighterState
{
    Idle,
    Walk,
    Jump,
    Attack,
    HitStun,
    BlockStun,
    KnockDown,
    KO
}

/// <summary>
/// キャラクターの状態を管理する。
/// </summary>
public class FighterStateMachine : MonoBehaviour
{
    [SerializeField]
    private FighterState initialState = FighterState.Idle;

    public FighterState CurrentState { get; private set; }

    public bool CanMove
    {
        get
        {
            return CurrentState == FighterState.Idle ||
                   CurrentState == FighterState.Walk ||
                   CurrentState == FighterState.Jump;
        }
    }

    public bool CanAttack
    {
        get
        {
            return CurrentState == FighterState.Idle ||
                   CurrentState == FighterState.Walk ||
                   CurrentState == FighterState.Jump;
        }
    }

    private void Awake()
    {
        CurrentState = initialState;
    }

    public bool TryChangeState(FighterState nextState)
    {
        // KO後に通常状態へ戻る事故を防ぐ
        if (CurrentState == FighterState.KO)
        {
            return false;
        }

        CurrentState = nextState;
        return true;
    }

    public void ForceChangeState(FighterState nextState)
    {
        CurrentState = nextState;
    }
}

