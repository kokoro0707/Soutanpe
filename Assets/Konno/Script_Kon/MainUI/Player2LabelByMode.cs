using TMPro;
using UnityEngine;

/// <summary>
/// 対戦シーンで、P2側のラベルをモードに合わせて切り替える。
/// プレイヤー同士 → "P2" / CPU戦 → "CPU"
/// 赤丸のテキスト(P2と表示されているTMP_Text)にアタッチ(または下のLabelに指定)する。
/// モードは GameModePanel が GameModeManager.CurrentMode に保存したものを読む。
/// </summary>
public class Player2LabelByMode : MonoBehaviour
{
    [Tooltip("空ならこのオブジェクトのTMP_Textを使う")]
    [SerializeField] private TMP_Text label;

    [SerializeField] private string playerVsPlayerText = "P2";
    [SerializeField] private string playerVsCpuText = "CPU";

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        Apply();
    }

    public void Apply()
    {
        if (label == null) return;

        if (GameModeManager.Instance == null)
        {
            // モード未設定(対戦シーン単体で再生した時など)は、今の表示のまま
            return;
        }

        label.text = GameModeManager.Instance.CurrentMode == GameModeManager.Mode.PlayerVsCPU
            ? playerVsCpuText
            : playerVsPlayerText;
    }
}
