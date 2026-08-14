using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 設定画面上部の3つのタブを管理する。
/// ・LBボタン(左肩ボタン) … 左のタブへ移動
/// ・RBボタン(右肩ボタン) … 右のタブへ移動
///
/// tabButtons と tabPanels は同じ並び順で対応させる(例: index 0 = 左端のタブ)。
/// タブを切り替えると、対応するパネルだけがアクティブになり、それ以外は非アクティブになる。
///
/// PCデバッグ用キー: Qキー = LB, Eキー = RB
/// </summary>
public class SettingsTabController : MonoBehaviour
{
    [Header("タブボタン(左から順に3つ)")]
    [SerializeField] private SettingsTabButton[] tabButtons;

    [Header("対応するパネル(tabButtonsと同じ並び順、3つ)")]
    [SerializeField] private GameObject[] tabPanels;

    [Header("開いたときに最初に表示するタブ")]
    [Tooltip("例: 3つ中の真ん中(サウンド設定)を最初に開きたいなら 1 にする")]
    [SerializeField] private int defaultTabIndex = 0;

    [Header("閉じる処理")]
    [SerializeField] private MainMenuManager menu;

    private int currentTabIndex;

    private void OnEnable()
    {
        int max = Mathf.Max(0, tabButtons.Length - 1);
        currentTabIndex = Mathf.Clamp(defaultTabIndex, 0, max);
        ApplyTabState();
    }

    private void Update()
    {
        if (ReadLB()) MoveTab(-1);
        else if (ReadRB()) MoveTab(1);

        if (ReadClose())
        {
            Close();
        }
    }
    private bool ReadClose()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
        var gp = Gamepad.current;
        if (gp != null && gp.buttonEast.wasPressedThisFrame) return true;
        return false;
    }

    private void Close()
    {
        gameObject.SetActive(false); // SettingsRoot自身を非表示に
        menu.CloseSettings();
    }

    private void MoveTab(int direction)
    {
        if (tabButtons.Length == 0) return;

        currentTabIndex = (currentTabIndex + direction + tabButtons.Length) % tabButtons.Length;
        ApplyTabState();
    }

    /// <summary>
    /// 現在選択中のタブに応じて、ボタンの色とパネルの表示/非表示をまとめて反映する。
    /// </summary>
    private void ApplyTabState()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null)
            {
                tabButtons[i].SetSelected(i == currentTabIndex);
            }
        }

        for (int i = 0; i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null)
            {
                tabPanels[i].SetActive(i == currentTabIndex);
            }
        }
    }

    private bool ReadLB()
    {
        var gp = Gamepad.current;
        if (gp != null && gp.leftShoulder.wasPressedThisFrame) return true;

        var kb = Keyboard.current;
        if (kb != null && kb.qKey.wasPressedThisFrame) return true;

        return false;
    }

    private bool ReadRB()
    {
        var gp = Gamepad.current;
        if (gp != null && gp.rightShoulder.wasPressedThisFrame) return true;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame) return true;

        return false;
    }
}