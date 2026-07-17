using UnityEngine;
using UnityEngine.InputSystem;

public class DebugBattle : MonoBehaviour
{
    [SerializeField] private Fighter player;
    [SerializeField] private Fighter enemy;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // Playerに30ダメージ
        if (keyboard.qKey.wasPressedThisFrame)
            player.Damage(30);

        // Enemyに30ダメージ
        if (keyboard.eKey.wasPressedThisFrame)
            enemy.Damage(30);

        // Player回復
        if (keyboard.aKey.wasPressedThisFrame)
            player.Heal(20);

        // Enemy回復
        if (keyboard.dKey.wasPressedThisFrame)
            enemy.Heal(20);

        // 全回復
        if (keyboard.rKey.wasPressedThisFrame)
        {
            player.Heal(player.maxHP);
            enemy.Heal(enemy.maxHP);
        }
    }
}