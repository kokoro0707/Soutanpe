using System.Collections;
using PersonaMenuUI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class MainMenuManager : MonoBehaviour
{
    [Header("メインメニュー")]
    [SerializeField] private TMP_Text[] menuTexts;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private string nextScene = "CharacterSelection";
    [Tooltip("3番目の項目(旧Exit)で戻る、タイトルシーンの名前")]
    [SerializeField] private string titleScene = "Title";
    [Header("斜めカーソル")]
    [Tooltip("SlantedRectで作った斜めカーソルを制御するコンポーネント。未設定でも動作する(その場合はカーソル演出なし)。")]
    [SerializeField] private MenuCursorSelector cursor;
    [Header("キーボード操作")]
    [Tooltip("決定として扱うキーボードのキー(複数指定可)")]
    [SerializeField] private Key[] keyboardDecideKeys = { Key.A, Key.Enter, Key.Space };
    [Header("SE")]
    [SerializeField] private AudioClip moveSe;
    [SerializeField] private AudioClip decideSe;
    [Header("スティック操作")]
    [Tooltip("ONにすると、左スティックでも左右に項目を移動できる(キー・十字キーも従来どおり使える)")]
    [SerializeField] private bool useStick = true;
    [Tooltip("スティックを何割倒したら入力として扱うか(0?1)")]
    [SerializeField, Range(0.1f, 0.95f)] private float stickThreshold = 0.6f;
    [Tooltip("倒したままの時に連続で移動させるか")]
    [SerializeField] private bool stickRepeat = true;
    [Tooltip("倒してから連続移動が始まるまでの時間(秒)")]
    [SerializeField] private float stickRepeatDelay = 0.4f;
    [Tooltip("連続移動の間隔(秒)")]
    [SerializeField] private float stickRepeatInterval = 0.15f;

    private readonly StickNavigator stick = new StickNavigator();
    private int currentIndex = 0;
    private bool inputLock;
    private void Start()
    {
        UpdateSelection();
    }
    private void Update()
    {
        Move();
        // 設定パネルが開いている間(inputLock中)は、
        // メインメニュー側の決定操作を一切受け付けない
        if (inputLock) return;
        bool submit =
            IsKeyboardDecidePressed() ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (submit)
        {
            PlaySe(decideSe);
            Execute();
        }
    }
    private void OpenSettings()
    {
        inputLock = true;
        settingsPanel.SetActive(true);
    }
    /// <summary>
    /// 設定パネル側(SettingsNavigatorのOn Closedイベント)から呼ばれる。
    /// メインメニューの操作を再開する。
    /// パネル自体の非表示化はSettingsNavigator.ClosePanel()側で行っている。
    /// </summary>
    public void CloseSettings()
    {
        inputLock = false;
    }
    private void Move()
    {
        // スティックの状態は、入力ロック中も毎フレーム更新しておく
        // (設定パネルを閉じた直後に、倒しっぱなしのスティックで勝手に動かないようにするため)
        stick.Threshold = stickThreshold;
        stick.UseRepeat = stickRepeat;
        stick.RepeatDelay = stickRepeatDelay;
        stick.RepeatInterval = stickRepeatInterval;
        stick.Poll();

        if (inputLock) return;

        bool stickLeft = useStick && stick.LeftPressed;
        bool stickRight = useStick && stick.RightPressed;

        var kb = Keyboard.current;
        // A/D キーが「決定キー」にも登録されている場合は、移動には使わない
        // (Aを押すと「左に移動」と「決定」が同じフレームで起き、隣の項目が実行されてしまうため)
        bool left = (kb != null && kb.leftArrowKey.wasPressedThisFrame) ||
            (kb != null && !IsDecideKey(Key.A) && kb.aKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame) ||
            stickLeft;
        bool right = (kb != null && kb.rightArrowKey.wasPressedThisFrame) ||
            (kb != null && !IsDecideKey(Key.D) && kb.dKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame) ||
            stickRight;
        if (left)
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = menuTexts.Length - 1;
            PlaySe(moveSe);
            UpdateSelection();
        }
        if (right)
        {
            currentIndex++;
            if (currentIndex >= menuTexts.Length)
                currentIndex = 0;
            PlaySe(moveSe);
            UpdateSelection();
        }
    }
    /// <summary>
    /// keyboardDecideKeys に登録されたキーのいずれかが押されたかを判定する。
    /// </summary>
    private bool IsKeyboardDecidePressed()
    {
        if (Keyboard.current == null) return false;

        for (int i = 0; i < keyboardDecideKeys.Length; i++)
        {
            if (Keyboard.current[keyboardDecideKeys[i]].wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }
    private bool IsDecideKey(Key key)
    {
        for (int i = 0; i < keyboardDecideKeys.Length; i++)
            if (keyboardDecideKeys[i] == key) return true;
        return false;
    }

    private void PlaySe(AudioClip clip)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }
    private void UpdateSelection()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            if (i == currentIndex)
            {
                menuTexts[i].color = Color.black;
                menuTexts[i].fontSize = 70;
            }
            else
            {
                menuTexts[i].color = Color.white;
                menuTexts[i].fontSize = 50;
            }
        }

        // 斜めカーソルを現在の選択位置へ移動させる。
        // MenuCursorSelector側の Use Internal Input / Follow Event System Selection は
        // OFFにしておき、選択の主導権はこのMainMenuManagerが持つ。
        if (cursor != null) cursor.Select(currentIndex);
    }
    private void Execute()
    {
        switch (currentIndex)
        {
            case 0:
                inputLock = true;
                StartCoroutine(StartGameRoutine());
                break;
            case 1:
                OpenSettings();
                break;
            case 2:
                // タイトルに戻る
                inputLock = true;
                ReturnToTitle();
                break;
        }
    }
    private IEnumerator StartGameRoutine()
    {
        yield return LoadSceneRoutine(nextScene);
    }

    /// <summary>
    /// タイトルへ戻る。
    /// タイトルシーン側には「フェードイン」を呼ぶ処理が無いため、StartFadeOut()だけで
    /// 切り替えると真っ黒なままになる。FadeToScene()(暗転→切替→明転まで行う)を使う。
    /// </summary>
    private void ReturnToTitle()
    {
        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeToScene(titleScene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(titleScene);
    }

    /// <summary>
    /// フェードアウトしてから指定シーンへ切り替える。
    /// </summary>
    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // タイトルと同じフェードアウト(FadeManagerが無い場合はそのまま切り替え)
        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.StartFadeOut();
        // シーン切替
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}