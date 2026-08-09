using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 設定画面上部のタブボタン1つ分の見た目を管理する。
/// ・選択中  : 文字色 白 / 背景色 赤
/// ・非選択時: 文字色・背景色ともに通常色(Inspectorで指定)
/// </summary>
public class SettingsTabButton : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image background;

    [Header("選択中の色")]
    [SerializeField] private Color selectedTextColor = Color.white;
    [SerializeField] private Color selectedBackgroundColor = new Color(0.85f, 0.15f, 0.15f); // 赤

    [Header("非選択時の色")]
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color normalBackgroundColor = Color.white;

    /// <summary>
    /// このタブが選択中かどうかで見た目を切り替える。SettingsTabControllerから呼ばれる。
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (label != null)
        {
            label.color = selected ? selectedTextColor : normalTextColor;
        }

        if (background != null)
        {
            background.color = selected ? selectedBackgroundColor : normalBackgroundColor;
        }
    }
}