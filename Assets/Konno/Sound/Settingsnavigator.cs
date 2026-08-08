using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// サウンド設定画面の操作。
/// ・十字キー 上/下 … BGM/SE/Voiceの行を移動
/// ・十字キー 左/右 … 選択中の行の音量を増減
/// ・Xボタン        … 選択中の行のミュートON/OFF切り替え
/// ・Bボタン        … 設定パネルを閉じる
/// ・LB/RB          … 設定タブの切り替え(今回はサウンド設定のみなので枠だけ用意)
///
/// PCデバッグ用キー: 矢印キー = 十字キー, Xキー = Xボタン, Escapeキー = Bボタン(閉じる),
///                   Qキー = LB, Eキー = RB
/// </summary>
public class SettingsNavigator : MonoBehaviour
{
    [Header("操作対象の行(上から順にBGM/SE/Voiceなど)")]
    [SerializeField] private VolumeChannelRow[] rows;

    [Header("入力の連射間隔")]
    [Tooltip("十字キーを押しっぱなしにしたときの連続入力の間隔(秒)")]
    [SerializeField] private float repeatInterval = 0.15f;
    [Tooltip("最初の入力から連射が始まるまでの遅延(秒)")]
    [SerializeField] private float repeatDelay = 0.4f;

    [Header("開閉時の通知(メインメニュー側の入力ロック解除などに使う)")]
    [Tooltip("この設定パネルが閉じられた時に呼ばれる(メインメニュー側の操作再開などに使う)")]
    [SerializeField] private UnityEvent onClosed;

    private int selectedIndex;
    private float verticalTimer;
    private float horizontalTimer;
    private bool verticalHeld;
    private bool horizontalHeld;

    private void OnEnable()
    {
        selectedIndex = 0;
        UpdateFocusVisual();

        foreach (var row in rows)
        {
            if (row != null) row.SyncFromAudioManager();
        }
    }

    private void Update()
    {
        Vector2 dpad = ReadDPad();
        bool togglePressed = ReadButtonDown(ButtonKind.West);  // ON/OFF切り替え(Xボタン)
        bool closePressed = ReadButtonDown(ButtonKind.East);   // 閉じる(Bボタン)

        if (closePressed)
        {
            ClosePanel();
            return;
        }

        HandleVertical(dpad.y);
        HandleHorizontal(dpad.x);

        if (togglePressed && rows.Length > 0 && rows[selectedIndex] != null)
        {
            rows[selectedIndex].ToggleMute();
        }
    }

    /// <summary>
    /// 設定パネルを閉じる。パネル自身を非アクティブにし、外部(メインメニュー側)へ通知する。
    /// UIのCloseボタンからも直接呼べる。
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        onClosed?.Invoke();
    }

    private void HandleVertical(float y)
    {
        bool held = Mathf.Abs(y) > 0.5f;

        if (!held)
        {
            verticalHeld = false;
            verticalTimer = 0f;
            return;
        }

        int direction = y > 0 ? -1 : 1; // 上入力で上の行(index-1)、下入力で下の行(index+1)

        if (!verticalHeld)
        {
            verticalHeld = true;
            verticalTimer = repeatDelay;
            MoveSelection(direction);
            return;
        }

        verticalTimer -= Time.deltaTime;
        if (verticalTimer <= 0f)
        {
            verticalTimer = repeatInterval;
            MoveSelection(direction);
        }
    }

    private void HandleHorizontal(float x)
    {
        bool held = Mathf.Abs(x) > 0.5f;

        if (!held)
        {
            horizontalHeld = false;
            horizontalTimer = 0f;
            return;
        }

        int direction = x > 0 ? 1 : -1;

        if (!horizontalHeld)
        {
            horizontalHeld = true;
            horizontalTimer = repeatDelay;
            AdjustSelected(direction);
            return;
        }

        horizontalTimer -= Time.deltaTime;
        if (horizontalTimer <= 0f)
        {
            horizontalTimer = repeatInterval;
            AdjustSelected(direction);
        }
    }

    private void MoveSelection(int direction)
    {
        if (rows.Length == 0) return;

        selectedIndex = (selectedIndex + direction + rows.Length) % rows.Length;
        UpdateFocusVisual();
    }

    private void AdjustSelected(int direction)
    {
        if (rows.Length == 0 || rows[selectedIndex] == null) return;

        if (direction > 0) rows[selectedIndex].Increase();
        else rows[selectedIndex].Decrease();
    }

    private void UpdateFocusVisual()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] != null) rows[i].SetFocused(i == selectedIndex);
        }
    }

    /// <summary>
    /// ゲームパッドの十字キー(またはスティック)+PCデバッグ用矢印キーを合成して読み取る。
    /// </summary>
    private Vector2 ReadDPad()
    {
        Vector2 result = Vector2.zero;

        var gp = Gamepad.current;
        if (gp != null)
        {
            result = gp.dpad.ReadValue();
            if (result == Vector2.zero)
            {
                result = gp.leftStick.ReadValue();
            }
        }

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.upArrowKey.isPressed) result.y = 1f;
            else if (kb.downArrowKey.isPressed) result.y = -1f;

            if (kb.rightArrowKey.isPressed) result.x = 1f;
            else if (kb.leftArrowKey.isPressed) result.x = -1f;
        }

        return result;
    }

    private enum ButtonKind { South, East, West, North }

    /// <summary>
    /// ボタンが押された瞬間を検知する。
    /// South: ゲームパッド Aボタン相当
    /// East : ゲームパッド Bボタン相当(閉じる)
    /// West : ゲームパッド Xボタン相当(ON/OFF切り替え)
    /// North: ゲームパッド Yボタン相当(未使用、今後の拡張用)
    ///
    /// PCデバッグキー: West→Xキー, East→Escapeキー, South→Zキー, North→Cキー
    /// </summary>
    private bool ReadButtonDown(ButtonKind kind)
    {
        var gp = Gamepad.current;
        if (gp != null)
        {
            switch (kind)
            {
                case ButtonKind.South: if (gp.buttonSouth.wasPressedThisFrame) return true; break;
                case ButtonKind.East: if (gp.buttonEast.wasPressedThisFrame) return true; break;
                case ButtonKind.West: if (gp.buttonWest.wasPressedThisFrame) return true; break;
                case ButtonKind.North: if (gp.buttonNorth.wasPressedThisFrame) return true; break;
            }
        }

        var kb = Keyboard.current;
        if (kb != null)
        {
            switch (kind)
            {
                case ButtonKind.South: if (kb.zKey.wasPressedThisFrame) return true; break;
                case ButtonKind.East: if (kb.escapeKey.wasPressedThisFrame) return true; break;
                case ButtonKind.West: if (kb.xKey.wasPressedThisFrame) return true; break;
                case ButtonKind.North: if (kb.cKey.wasPressedThisFrame) return true; break;
            }
        }

        return false;
    }
}