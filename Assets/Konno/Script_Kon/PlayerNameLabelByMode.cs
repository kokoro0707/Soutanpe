using TMPro;
using UnityEngine;

/// <summary>
/// ゲームモードに応じてテキストを切り替える。
/// プレイヤー同士(PlayerVsPlayer)なら「P2」、CPU戦(PlayerVsCPU)なら「CPU」と表示する。
/// 対戦シーンの「P2」テキストにアタッチして使う(TMP_Textと同じオブジェクト)。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class PlayerNameLabelByMode : MonoBehaviour
{
    [SerializeField] private string playerVsPlayerText = "P2";
    [SerializeField] private string playerVsCpuText = "CPU";

    [Tooltip("GameModeManagerが見つからない時(シーン単体テストなど)に使うテキスト")]
    [SerializeField] private string fallbackText = "P2";

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        if (label == null) label = GetComponent<TMP_Text>();

        if (GameModeManager.Instance == null)
        {
            label.text = fallbackText;
            return;
        }

        label.text = GameModeManager.Instance.CurrentMode == GameModeManager.Mode.PlayerVsCPU
            ? playerVsCpuText
            : playerVsPlayerText;
    }
}
