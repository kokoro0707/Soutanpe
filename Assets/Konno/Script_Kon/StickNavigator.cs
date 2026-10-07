using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゲームパッドの左スティック(任意で十字キーとは別に)を、
/// 「キーを1回押した」のような上下左右の入力として扱うための小さな部品。
/// メニューの左右移動などで、キー入力に「加えて」スティックでも動かしたい時に使う。
///
/// 使い方(例: MainMenuManager内):
///   1. フィールドを追加:   private StickNavigator stick = new StickNavigator();
///   2. Update()の最初で:   stick.Poll();
///   3. 既存のキー判定に || を足す:
///        bool left  = (キーの左判定) || stick.LeftPressed;
///        bool right = (キーの右判定) || stick.RightPressed;
///      上下も同様に stick.UpPressed / stick.DownPressed。
///
/// 仕様:
///   ・スティックを倒しきる手前(Threshold)を超えた瞬間に1回だけ「押した」扱いになる
///   ・倒したままにすると、Repeat Delay後からRepeat Intervalごとに連続入力になる
///     (Repeatを使いたくない場合はuseRepeatをfalseに)
///   ・斜めに倒した時は、より大きく倒した軸の方向だけを入力として扱う
///   ・Time.timeScaleが0でも動く(unscaledTime使用)
/// </summary>
public class StickNavigator
{
    public float Threshold = 0.6f;
    public bool UseRepeat = true;
    public float RepeatDelay = 0.4f;
    public float RepeatInterval = 0.15f;

    public bool LeftPressed { get; private set; }
    public bool RightPressed { get; private set; }
    public bool UpPressed { get; private set; }
    public bool DownPressed { get; private set; }

    private int heldDirection = 0;     // 0=なし 1=左 2=右 3=上 4=下
    private float nextRepeatTime;

    /// <summary>毎フレーム1回、Update()内で呼ぶ。</summary>
    public void Poll()
    {
        LeftPressed = RightPressed = UpPressed = DownPressed = false;

        Gamepad pad = Gamepad.current;
        if (pad == null)
        {
            heldDirection = 0;
            return;
        }

        Vector2 v = pad.leftStick.ReadValue();

        int dir = 0;
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
        {
            if (v.x <= -Threshold) dir = 1;
            else if (v.x >= Threshold) dir = 2;
        }
        else
        {
            if (v.y >= Threshold) dir = 3;
            else if (v.y <= -Threshold) dir = 4;
        }

        if (dir == 0)
        {
            heldDirection = 0;
            return;
        }

        bool fire = false;

        if (dir != heldDirection)
        {
            // 新しく倒した(または向きが変わった)瞬間
            heldDirection = dir;
            nextRepeatTime = Time.unscaledTime + RepeatDelay;
            fire = true;
        }
        else if (UseRepeat && Time.unscaledTime >= nextRepeatTime)
        {
            nextRepeatTime = Time.unscaledTime + RepeatInterval;
            fire = true;
        }

        if (!fire) return;

        switch (dir)
        {
            case 1: LeftPressed = true; break;
            case 2: RightPressed = true; break;
            case 3: UpPressed = true; break;
            case 4: DownPressed = true; break;
        }
    }
}
