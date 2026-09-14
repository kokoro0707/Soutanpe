using UnityEngine;

public class TutorialP2InputSource : MonoBehaviour, IFighterInputSource
{
    [SerializeField]
    private float attackInterval = 3.0f;

    private float attackTimer = 0.0f;

    public FighterInputData ReadInput()
    {
        FighterInputData input = new FighterInputData();

        input.horizontal = 0;
        input.vertical = 0;

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackInterval)
        {
            attackTimer = 0.0f;

            input.lightAttackPressed = true;

            Debug.Log("P2が弱攻撃！");
        }

        return input;
    }
}