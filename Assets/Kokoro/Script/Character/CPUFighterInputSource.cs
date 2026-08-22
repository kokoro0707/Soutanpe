using UnityEngine;

/// <summary>
/// CPU用の入力処理。
/// プレイヤー入力の代わりにFighterInputDataを生成する。
/// </summary>
public sealed class CPUFighterInputSource :
    MonoBehaviour,
    IFighterInputSource
{
    [Header("相手")]
    [SerializeField]
    private Transform opponent;


    [Header("距離設定")]

    // この距離より遠ければ相手に近づく
    [SerializeField, Min(0f)]
    private float attackDistance = 1.6f;

    // 必殺技を使える距離の目安
    [SerializeField, Min(0f)]
    private float specialDistance = 4f;


    [Header("CPUの判断速度")]

    // 小さいほど頻繁に行動を選ぶ
    [SerializeField, Min(0.05f)]
    private float decisionInterval = 0.25f;


    [Header("行動確率")]

    [SerializeField, Range(0f, 1f)]
    private float guardChance = 0.15f;

    [SerializeField, Range(0f, 1f)]
    private float grabChance = 0.15f;

    [SerializeField, Range(0f, 1f)]
    private float heavyAttackChance = 0.20f;

    [SerializeField, Range(0f, 1f)]
    private float assistComboChance = 0.10f;

    [SerializeField, Range(0f, 1f)]
    private float specialChance = 0.15f;

    [SerializeField, Range(0f, 1f)]
    private float jumpChance = 0.05f;


    [Header("ガード")]

    [SerializeField, Min(0.05f)]
    private float guardDuration = 0.5f;


    private float nextDecisionTime;
    private float guardEndTime;


    /// <summary>
    /// BattleModeSetupからCPUの相手を設定する。
    /// </summary>
    public void SetOpponent(
        Transform newOpponent
    )
    {
        opponent = newOpponent;
    }


    /// <summary>
    /// FighterControllerから毎フレーム呼ばれる。
    /// </summary>
    public FighterInputData ReadInput()
    {
        FighterInputData input =
            new FighterInputData();


        if (opponent == null)
        {
            return input;
        }


        float differenceX =
            opponent.position.x -
            transform.position.x;

        float distance =
            Mathf.Abs(differenceX);


        // 相手が右なら1
        // 相手が左なら-1
        int towardDirection =
            differenceX >= 0f
                ? 1
                : -1;


        // =====================================
        // ガード継続中
        // =====================================

        if (Time.time < guardEndTime)
        {
            // 相手と逆方向を入力するとガード
            input.horizontal =
                -towardDirection;

            return input;
        }


        // =====================================
        // 判断待ち中
        // =====================================

        if (Time.time <
            nextDecisionTime)
        {
            // 遠ければ近づき続ける
            if (distance >
                attackDistance)
            {
                input.horizontal =
                    towardDirection;
            }

            return input;
        }


        nextDecisionTime =
            Time.time +
            decisionInterval;


        // =====================================
        // 遠距離
        // =====================================

        if (distance >
            specialDistance)
        {
            // 基本は相手に近づく
            input.horizontal =
                towardDirection;


            // たまにジャンプ
            if (Random.value <
                jumpChance)
            {
                input.jumpPressed =
                    true;
            }

            return input;
        }


        // =====================================
        // 中距離
        // =====================================

        if (distance >
            attackDistance)
        {
            // 横必殺技
            if (Random.value <
                specialChance)
            {
                input.horizontal =
                    towardDirection;

                input.specialAttackPressed =
                    true;

                return input;
            }


            // それ以外は近づく
            input.horizontal =
                towardDirection;

            return input;
        }


        // =====================================
        // 近距離
        // =====================================

        float random =
            Random.value;


        // ガード
        if (random <
            guardChance)
        {
            guardEndTime =
                Time.time +
                guardDuration;

            input.horizontal =
                -towardDirection;

            return input;
        }

        random -= guardChance;


        // つかみ
        if (random <
            grabChance)
        {
            input.grabPressed =
                true;

            return input;
        }

        random -= grabChance;


        // 強攻撃
        if (random <
            heavyAttackChance)
        {
            input.heavyAttackPressed =
                true;

            return input;
        }

        random -= heavyAttackChance;


        // アシストコンボ
        if (random <
            assistComboChance)
        {
            input.assistComboPressed =
                true;

            return input;
        }

        random -= assistComboChance;


        // 必殺技
        if (random <
            specialChance)
        {
            // 相手方向 + 必殺技
            // FighterCommandInterpreter側で
            // ForwardSpecialになる
            input.horizontal =
                towardDirection;

            input.specialAttackPressed =
                true;

            return input;
        }


        // =====================================
        // 何も選ばれなかったら弱攻撃
        // =====================================

        input.lightAttackPressed =
            true;

        return input;
    }
}
