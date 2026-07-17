using UnityEngine;

/// <summary>
/// Player / Enemy のHPをまとめて確認・操作できるデバッグ用HUD。
/// 空のGameObjectを作ってアタッチし、Inspectorで Player / Enemy の
/// HealthSystem を割り当てる。
/// </summary>
public class DebugHUD : MonoBehaviour
{
    [SerializeField] private HealthSystem playerHealth;
    [SerializeField] private HealthSystem enemyHealth;
    [SerializeField] private int testDamage = 10;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 260, 230), GUI.skin.box);
        GUILayout.Label("=== デバッグHUD ===");

        DrawHealthBar("Player", playerHealth);
        GUILayout.Space(10);
        DrawHealthBar("Enemy", enemyHealth);

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"Playerに{testDamage}") && playerHealth != null)
            playerHealth.TakeDamage(testDamage);
        if (GUILayout.Button($"Enemyに{testDamage}") && enemyHealth != null)
            enemyHealth.TakeDamage(testDamage);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("HPリセット"))
        {
            playerHealth?.ResetHP();
            enemyHealth?.ResetHP();
        }

        GUILayout.EndArea();
    }

    private void DrawHealthBar(string label, HealthSystem hs)
    {
        if (hs == null)
        {
            GUILayout.Label($"{label}: 未割り当て");
            return;
        }

        GUILayout.Label($"{label} HP: {hs.CurrentHP} / {hs.MaxHP} {(hs.IsDead ? "(死亡)" : "")}{(hs.IsGuarding ? " [ガード中]" : "")}");
        Rect barRect = GUILayoutUtility.GetRect(200, 20);
        GUI.Box(barRect, "");
        float ratio = hs.MaxHP > 0 ? (float)hs.CurrentHP / hs.MaxHP : 0;
        GUI.color = ratio > 0.5f ? Color.green : (ratio > 0.2f ? Color.yellow : Color.red);
        GUI.Box(new Rect(barRect.x, barRect.y, barRect.width * ratio, barRect.height), "");
        GUI.color = Color.white;
    }
}