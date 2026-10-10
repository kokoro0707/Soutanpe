using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("キャラクターアイコン")]
    [SerializeField] private Image[] characterIcons; // キャラクターアイコンのImageコンポーネントを配列で設定する
    [Tooltip("各アイコンに付けたOutlineコンポーネント(characterIconsと同じ順番・同じ数)。選択中は色を塗るのではなく、この枠線で囲んで表示する。")]
    [SerializeField] private Outline[] characterIconOutlines;

    [Header("選択フレーム(PNG)")]
    [Tooltip("P1用フレーム画像(青)。CharFrame_P1_Blue.png をSpriteに設定したImageを1つ用意して割り当てる。" +
             "選択中のアイコンの位置・大きさに自動で移動する。Raycast Targetはオフ推奨")]
    [SerializeField] private Image player1Frame;
    [Tooltip("P2/CPU用フレーム画像(赤)。CharFrame_P2_Red.png を設定したImage")]
    [SerializeField] private Image player2Frame;
    [Tooltip("フレームをアイコンよりどれだけ外側に広げるか(ピクセル)")]
    [SerializeField] private float framePadding = 4f;
    [Tooltip("P1とP2が同じキャラを選んだ時、P2のフレームをさらに外側へ広げる量(重なり防止)")]
    [SerializeField] private float overlapExtraPadding = 14f;
    [Tooltip("決定後のフレームの色(Imageの色として乗算される。白=元の色のまま)")]
    [SerializeField] private Color frameDecidedTint = new Color(0.65f, 0.65f, 0.65f, 1f);

    [Header("カラー設定")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color player1Color = Color.red;
    [SerializeField] private Color player2Color = Color.blue;
    [SerializeField] private Color decidedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("ラベル")]
    [SerializeField] private TMP_Text player1Label;
    [SerializeField] private TMP_Text player2Label;

    [Header("戻るボタン")]
    [Tooltip("Escape/Bボタンを押した時の挙動を示すテキスト。" +
             "キャラ選択前(メインメニューに戻る)と選択後(決定の取消)で表示を切り替える")]
    [SerializeField] private TMP_Text backButtonLabel;

    [Header("立ち絵")]
    [SerializeField] private Image player1Preview;
    [SerializeField] private Image player2Preview;

    [SerializeField] private GameObject gameModePanel;  // ゲームモード選択パネル(Player vs Player / Player vs CPU)
    [SerializeField] private GameObject characterRoot;  // キャラクター選択パネルのルートオブジェクト
    [SerializeField] private GameModePanel gameModeManagerPanel;

    [Header("バトル用キャラクターデータ")]
    [SerializeField]
    private FighterCharacterData[] characterDataList;
    [Header("キャラクターアイコン画像")]
    [SerializeField] private Sprite[] characterIconSprites;
    [Header("キャラクター表示画像")]
    [SerializeField] private Sprite[] characterPreviewSprites;
    [Header("バトル画面のHP横に出す顔画像 (任意)")]
    [Tooltip("characterDataListと同じ順番。未設定(または該当の要素がnull)なら、キャラクターアイコン画像を顔として使う")]
    [SerializeField] private Sprite[] characterFaceSprites;

    [Header("バトルシーン")]
    [SerializeField] private string battleSceneName = "Character";

    [Header("SE")]
    [SerializeField] private AudioClip moveSe;   // カーソル移動音
    [SerializeField] private AudioClip decideSe; // 決定音
    [SerializeField] private AudioClip cancelSe; // 取消・戻る音

    [Header("キーボード操作(Player1用)")]
    [Tooltip("Player1の移動キー(左)")]
    [SerializeField] private Key keyboardLeftKey = Key.LeftArrow;
    [Tooltip("Player1の移動キー(右)")]
    [SerializeField] private Key keyboardRightKey = Key.RightArrow;
    [Tooltip("Player1の決定キー(複数指定可)")]
    [SerializeField] private Key[] keyboardDecideKeys = { Key.Enter, Key.Space };
    [Tooltip("Player1の取消キー(決定の取り消しなど、ゲームパッドのBボタン相当)")]
    [SerializeField] private Key keyboardCancelKey = Key.Backspace;

    [Header("スティック操作")]
    [Tooltip("ONにすると、左スティックの左右でもカーソルを移動できる。" +
             "P1(とCPU選択・確認パネル)は1台目のパッド、P2は2台目のパッドのスティックで操作する。" +
             "キー・十字キーも従来どおり使える")]
    [SerializeField] private bool useStick = true;
    [SerializeField, Range(0.1f, 0.95f)] private float stickThreshold = 0.6f;
    [SerializeField] private bool stickRepeat = true;
    [SerializeField] private float stickRepeatDelay = 0.4f;
    [SerializeField] private float stickRepeatInterval = 0.15f;

    [Header("パッドの割り当て")]
    [Tooltip("ONの時、パッドがちょうど2台なら 1台目=P1 / 2台目=P2 にすぐ割り当てる。\n" +
             "OFFの時(または3台以上の時)は、最初に操作したパッドがP1、次に操作した別のパッドがP2。\n" +
             "Steamの仮想パッド等で、見えているパッドの数が実際より多い場合はOFFにする")]
    [SerializeField] private bool autoAssignWhenTwoPads = true;

    private readonly StickNavigator stick1 = new StickNavigator();
    private readonly StickNavigator stick2 = new StickNavigator();

    [Header("バトル確認パネル")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;

    [Header("確認パネル選択肢")]
    [SerializeField] private TMP_Text[] confirmMenuTexts;

    [SerializeField] private Color confirmNormalColor = Color.white;
    [SerializeField] private Color confirmSelectColor = Color.red;

    private int confirmIndex = 0;
    private bool isConfirming = false;
    //[SerializeField] private Sprite[] characterSprites; 画像を使う場合はここに設定する

    private int player1Index = 0;
    private int player2Index = 0;

    private bool player1Decided;
    private bool player2Decided;

    private bool player2Active;

    private Gamepad player1Pad;
    private Gamepad player2Pad;
    private bool cpuMode;
    //private bool selectingCPU;
    private bool canInput = false;
    private bool previousCpuMode;
    private bool isChangingScene = false;

    [SerializeField]
    private Color[] characterColors =

    {
Color.red,
Color.blue,
Color.yellow,
Color.green
};
    private enum SelectState
    {
        Player1,
        Player2,
        CPU
    }

    private SelectState selectState = SelectState.Player1;
    private void OnEnable()
    {
        //UpdateGamepads();
        //UpdateSelectionColor();

        Initialize();
        //StartCoroutine(WaitReleaseButton());
    }
    void Start()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }
    private void PollSticks()
    {
        // P1/P2に割り当て済みのパッドのスティックを見る
        Gamepad pad1 = player1Pad;
        Gamepad pad2 = player2Pad;

        foreach (StickNavigator s in new[] { stick1, stick2 })
        {
            s.Threshold = stickThreshold;
            s.UseRepeat = stickRepeat;
            s.RepeatDelay = stickRepeatDelay;
            s.RepeatInterval = stickRepeatInterval;
        }

        stick1.Poll(pad1);
        stick2.Poll(pad2);
    }

    private void Update()
    {
        // パッドの割り当てを毎フレーム更新(確認パネル表示中も含む)
        UpdateGamepads();

        // スティックの状態は毎フレーム更新しておく(確認パネル表示中も含む)
        PollSticks();

        // パッドが今押されて割り当てられたフレームは、そのボタンで誤って決定等しないよう入力を捨てる
        if (padJoinedThisFrame)
            return;

        // 確認パネルが開いている間
        // 確認パネル表示中
        if (isConfirming)
        {
            ConfirmInput();
            return;
        }
        //UpdateGamepads();
        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
             (player1Pad != null &&
                player1Pad.buttonEast.wasPressedThisFrame &&
                selectState == SelectState.Player1 &&
                !player1Decided))
        {
            PlaySe(cancelSe);
            BackToGameMode();
            return;
        }
        cpuMode = GameModeManager.Instance.CurrentMode ==
             GameModeManager.Mode.PlayerVsCPU;

        if (cpuMode != previousCpuMode)
        {
            //selectingCPU = false;
            player2Decided = false;
            previousCpuMode = cpuMode;

            UpdateSelectionColor();
        }

        if (!canInput)
            return;

        if (cpuMode)
        {
            // CPU戦:P1が選ぶ → 続けてP1の操作でCPUのキャラを選ぶ(従来どおり)
            switch (selectState)
            {
                case SelectState.Player1:
                    Player1Input();
                    break;

                case SelectState.CPU:
                    CPUInput();
                    break;
            }
        }
        else
        {
            // 対人戦:P1とP2がそれぞれのパッドで「同時に」選べる
            Player1Input();

            if (player2Pad != null)
                Player2Input();
        }
    }

    private void PlaySe(AudioClip clip)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }

    // ===== キーボード+ゲームパッド共通の入力判定(Player1 / CPU選択 / 確認パネルで使用) =====

    // 確認パネル用:P1のパッドに加えて、P2のパッドでも操作できる
    private bool ConfirmLeft() => IsLeftPressed(player1Pad) || (player2Pad != null && (player2Pad.dpad.left.wasPressedThisFrame || (useStick && stick2.LeftPressed)));
    private bool ConfirmRight() => IsRightPressed(player1Pad) || (player2Pad != null && (player2Pad.dpad.right.wasPressedThisFrame || (useStick && stick2.RightPressed)));
    private bool ConfirmDecide() => IsDecidePressed(player1Pad) || (player2Pad != null && player2Pad.buttonSouth.wasPressedThisFrame);
    private bool ConfirmCancel() => IsCancelPressed(player1Pad) || (player2Pad != null && player2Pad.buttonEast.wasPressedThisFrame);

    private bool IsLeftPressed(Gamepad pad)
    {
        bool key = Keyboard.current != null &&
            (Keyboard.current[keyboardLeftKey].wasPressedThisFrame ||
             Keyboard.current.aKey.wasPressedThisFrame);
        bool gp = pad != null && pad.dpad.left.wasPressedThisFrame;
        bool st = useStick && stick1.LeftPressed;
        return key || gp || st;
    }

    private bool IsRightPressed(Gamepad pad)
    {
        bool key = Keyboard.current != null &&
            (Keyboard.current[keyboardRightKey].wasPressedThisFrame ||
             Keyboard.current.dKey.wasPressedThisFrame);
        bool gp = pad != null && pad.dpad.right.wasPressedThisFrame;
        bool st = useStick && stick1.RightPressed;
        return key || gp || st;
    }

    private bool IsDecidePressed(Gamepad pad)
    {
        bool gp = pad != null && pad.buttonSouth.wasPressedThisFrame;
        return IsKeyboardDecidePressed() || gp;
    }

    private bool IsCancelPressed(Gamepad pad)
    {
        bool key = Keyboard.current != null && Keyboard.current[keyboardCancelKey].wasPressedThisFrame;
        bool gp = pad != null && pad.buttonEast.wasPressedThisFrame;
        return key || gp;
    }

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

    private void BackToGameMode()
    {
        StartCoroutine(BackToGameModeRoutine());
    }

    private IEnumerator BackToGameModeRoutine()
    {
        // フェードアウト
        yield return FadeManager.Instance.StartFadeOut(0.2f);

        // パネル切り替え
        characterRoot.SetActive(false);
        gameModePanel.SetActive(true);

        // 初期化
        //gameModeManagerPanel.Initialize();

        // フェードイン
        FadeManager.Instance.StartFadeIn(0.2f);
    }
    // シーンをまたいでも覚えておくパッドの割り当て
    private static Gamepad assignedP1Pad;
    private static Gamepad assignedP2Pad;
    private bool padJoinedThisFrame;

    /// <summary>キャラ選択で決まったP1のパッド(バトルシーンの入力などで使う)</summary>
    public static Gamepad Player1Gamepad => assignedP1Pad != null && assignedP1Pad.added ? assignedP1Pad : null;
    /// <summary>キャラ選択で決まったP2のパッド</summary>
    public static Gamepad Player2Gamepad => assignedP2Pad != null && assignedP2Pad.added ? assignedP2Pad : null;

    /// <summary>
    /// パッドの割り当て。
    /// 以前は「1台目=P1、2台目=P2」で固定していたが、Steamの仮想パッド等が
    /// 1台目に入っていると、実際に握っているパッドが反応しなくなるため、
    /// ・パッドが1台だけ → そのパッドがP1
    /// ・複数台ある → 最初にボタン/十字キー/スティックを操作したパッドがP1、
    ///                その次に操作した別のパッドがP2
    /// にしている。
    /// </summary>
    private void UpdateGamepads()
    {
        padJoinedThisFrame = false;

        // 抜かれたパッドは割り当てを外す
        if (assignedP1Pad != null && !assignedP1Pad.added) assignedP1Pad = null;
        if (assignedP2Pad != null && !assignedP2Pad.added) assignedP2Pad = null;

        // ちょうど2台で、まだどちらも割り当てていない → 1台目=P1、2台目=P2 で即割り当て
        if (autoAssignWhenTwoPads && assignedP1Pad == null && assignedP2Pad == null && Gamepad.all.Count == 2)
        {
            assignedP1Pad = Gamepad.all[0];
            assignedP2Pad = Gamepad.all[1];
            Debug.Log($"[CharacterSelect] P1パッド = {assignedP1Pad.displayName} / P2パッド = {assignedP2Pad.displayName}");
        }

        // P1
        if (assignedP1Pad == null)
        {
            if (Gamepad.all.Count == 1 && Gamepad.all[0] != assignedP2Pad)
            {
                assignedP1Pad = Gamepad.all[0];
                Debug.Log($"[CharacterSelect] P1パッド = {assignedP1Pad.displayName}");
            }
            else
            {
                Gamepad used = FindPressedPad(assignedP2Pad);
                if (used != null)
                {
                    assignedP1Pad = used;
                    padJoinedThisFrame = true;
                    Debug.Log($"[CharacterSelect] P1パッド = {assignedP1Pad.displayName}");
                }
            }
        }

        // P2(P1とは別のパッドで操作されたら参加)
        if (assignedP2Pad == null && assignedP1Pad != null)
        {
            Gamepad used = FindPressedPad(assignedP1Pad);
            if (used != null)
            {
                assignedP2Pad = used;
                padJoinedThisFrame = true;
                Debug.Log($"[CharacterSelect] P2パッド = {assignedP2Pad.displayName}");
            }
        }

        player1Pad = assignedP1Pad;
        player2Pad = assignedP2Pad;

        bool active = player2Pad != null;
        if (active != player2Active)
        {
            player2Active = active;
            if (!player2Active)
            {
                player2Decided = false;
            }
            UpdateSelectionColor();
        }
    }

    // exclude 以外で、このフレームに操作されたパッドを探す
    private Gamepad FindPressedPad(Gamepad exclude)
    {
        foreach (Gamepad pad in Gamepad.all)
        {
            if (pad == null || pad == exclude) continue;

            bool pressed =
                pad.buttonSouth.wasPressedThisFrame ||
                pad.buttonEast.wasPressedThisFrame ||
                pad.buttonWest.wasPressedThisFrame ||
                pad.buttonNorth.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame ||
                pad.dpad.left.wasPressedThisFrame ||
                pad.dpad.right.wasPressedThisFrame ||
                pad.dpad.up.wasPressedThisFrame ||
                pad.dpad.down.wasPressedThisFrame ||
                pad.leftStick.ReadValue().magnitude > 0.8f;

            if (pressed) return pad;
        }
        return null;
    }

    private void Player1Input()
    {
        if (player1Decided)
        {
            // 対人戦では、決定済みでもBで自分の決定だけ取り消せる
            if (!cpuMode)
            {
                if (IsCancelPressed(player1Pad))
                {
                    player1Decided = false;
                    PlaySe(cancelSe);
                    UpdateSelectionColor();

                }
            }
            return;
        }

        if (IsLeftPressed(player1Pad))
        {
            player1Index--;
            if (player1Index < 0)
                player1Index = characterIcons.Length - 1;
            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (IsRightPressed(player1Pad))
        {
            player1Index++;
            if (player1Index >= characterIcons.Length)
                player1Index = 0;
            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (IsDecidePressed(player1Pad))
        {

            player1Decided = true;
            PlaySe(decideSe);

            if (CharacterSelectionData.Instance != null &&
                player1Index >= 0 &&
                player1Index < characterDataList.Length)
            {
                CharacterSelectionData.Instance.SetPlayer1Character(
                    characterDataList[player1Index]
                );
            }
            // 仮:Prefab保存なし

            if (cpuMode)
                selectState = SelectState.CPU;


            UpdateSelectionColor();

            // 両方決定したか確認
            CheckBothPlayersDecided();
        }
    }

    private void Player2Input()
    {
        if (player2Decided)
        {
            // 決定済みなら取消のみ受け付ける
            if (player2Pad.buttonEast.wasPressedThisFrame)
            {
                player2Decided = false;
                PlaySe(cancelSe);
                UpdateSelectionColor();

            }
            return;
        }

        if (player2Pad.dpad.left.wasPressedThisFrame || (useStick && stick2.LeftPressed))
        {
            player2Index--;
            if (player2Index < 0)
                player2Index = characterIcons.Length - 1;
            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (player2Pad.dpad.right.wasPressedThisFrame || (useStick && stick2.RightPressed))
        {
            player2Index++;
            if (player2Index >= characterIcons.Length)
                player2Index = 0;
            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (player2Pad.buttonSouth.wasPressedThisFrame)
        {
            player2Decided = true;
            PlaySe(decideSe);

            if (CharacterSelectionData.Instance != null &&
                player2Index >= 0 &&
                player2Index < characterDataList.Length)
            {
                CharacterSelectionData.Instance.SetPlayer2Character(
                    characterDataList[player2Index]
                );
            }

            // 仮:Prefab保存なし
            UpdateSelectionColor();
            // 両方決定したか確認
            CheckBothPlayersDecided();
        }
    }
    private void CPUInput()
    {
        // まだ1Pが決定していないなら何もしない
        if (!player1Decided)
            return;

        // 1P決定後にCPU選択開始
        //selectingCPU = true;

        if (IsLeftPressed(player1Pad))
        {
            player2Index--;

            if (player2Index < 0)
                player2Index = characterIcons.Length - 1;

            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (IsRightPressed(player1Pad))
        {
            player2Index++;

            if (player2Index >= characterIcons.Length)
                player2Index = 0;

            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (IsDecidePressed(player1Pad))
        {
            player2Decided = true;
            PlaySe(decideSe);

            if (CharacterSelectionData.Instance != null &&
                player2Index >= 0 &&
                player2Index < characterDataList.Length)
            {
                CharacterSelectionData.Instance.SetPlayer2Character(
                    characterDataList[player2Index]
                );
            }
            // 仮:Prefab保存なし

            UpdateSelectionColor();
            // 両方決定したか確認
            CheckBothPlayersDecided();
            // TODO : バトルシーンへ
        }
        // 取消
        if (IsCancelPressed(player1Pad))
        {
            if (player2Decided)
            {
                // CPU決定取消
                player2Decided = false;
                PlaySe(cancelSe);
                UpdateSelectionColor();

            }
            else if (selectState == SelectState.CPU)
            {
                // CPU選択をやめてP1選択へ戻る
                //selectingCPU = false;
                player1Decided = false;
                selectState = SelectState.Player1;
                PlaySe(cancelSe);
                UpdateSelectionColor();
            }
        }
    }
    private void UpdateSelectionColor()
    {
        // 全員、色は通常のまま。枠線もいったん全部消す
        foreach (Image icon in characterIcons)
        {
            icon.color = normalColor;
        }

        if (characterIconOutlines != null)
        {
            foreach (Outline outline in characterIconOutlines)
            {
                if (outline != null) outline.enabled = false;
            }
        }

        // ===== PLAYER1 =====
        player1Label.gameObject.SetActive(true);
        player1Label.text = "PLAYER";
        player1Label.color = player1Color;

        // 選択中のキャラの上へ移動
        player1Label.rectTransform.position =
            characterIcons[player1Index].rectTransform.position + new Vector3(0, 100, 0);

        SetIconOutline(player1Index, player1Decided ? decidedColor : player1Color);

        // ===== PLAYER2 / CPU =====
        if (cpuMode)
        {
            if (selectState == SelectState.CPU)
            {
                player2Label.gameObject.SetActive(true);
                player2Label.text = "CPU";
                player2Label.color = Color.blue;

                player2Label.rectTransform.position =
                    characterIcons[player2Index].rectTransform.position + new Vector3(0, 100, 0);

                SetIconOutline(player2Index, player2Decided ? decidedColor : Color.blue);
            }
            else
            {
                player2Label.gameObject.SetActive(false);
            }
        }
        else if (player2Active)
        {
            player2Label.gameObject.SetActive(true);
            player2Label.text = "PLAYER";
            player2Label.color = player2Color;

            player2Label.rectTransform.position =
                characterIcons[player2Index].rectTransform.position + new Vector3(0, 80, 0);

            SetIconOutline(player2Index, player2Decided ? decidedColor : player2Color);
        }
        else
        {
            player2Label.gameObject.SetActive(false);
        }
        UpdateFrames();
        UpdatePreview();
        UpdateBackButtonLabel();

        CheckBothPlayersDecided();
    }

    /// <summary>
    /// Escape/Bボタンを押した時に「メインメニューに戻る」のか「選択をキャンセル」なのかを
    /// 表すテキストを、現在の選択状態に合わせて切り替える。
    /// (実際の判定ロジックはUpdate()/Player1Input()側にあるので、ここは表示を合わせるだけ)
    /// </summary>
    private void UpdateBackButtonLabel()
    {
        if (backButtonLabel == null) return;

        // Player1がまだキャラを決定していない間だけ、Escape/Bでメインメニューに戻る(BackToGameMode)。
        // それ以降は、Escape/Bを押しても各決定の取消にしかならない。
        bool canReturnToMainMenu = selectState == SelectState.Player1 && !player1Decided;

        backButtonLabel.text = canReturnToMainMenu
            ? "モードに戻る"
            : "選択をキャンセル";
    }

    /// <summary>
    /// P1/P2(CPU)のフレーム画像を、選択中アイコンの位置・大きさに合わせる。
    /// </summary>
    private void UpdateFrames()
    {
        // P1: 常に表示
        PlaceFrame(player1Frame, player1Index, 0f, player1Decided);

        // P2 / CPU: 選択フェーズに入っている時だけ表示
        bool showP2 = cpuMode
            ? selectState == SelectState.CPU
            : player2Active;

        bool overlap = player1Index == player2Index;
        PlaceFrame(showP2 ? player2Frame : null, player2Index,
                   overlap ? overlapExtraPadding : 0f, player2Decided);

        if (!showP2 && player2Frame != null)
            player2Frame.gameObject.SetActive(false);
    }

    private void PlaceFrame(Image frame, int index, float extraPadding, bool decided)
    {
        if (frame == null) return;
        if (index < 0 || index >= characterIcons.Length || characterIcons[index] == null)
        {
            frame.gameObject.SetActive(false);
            return;
        }

        frame.gameObject.SetActive(true);

        RectTransform icon = characterIcons[index].rectTransform;
        RectTransform rt = frame.rectTransform;

        // アイコンと同じ見た目の位置・サイズに合わせる(親が違っても対応)
        rt.position = icon.position;
        rt.rotation = icon.rotation;
        float pad = (framePadding + extraPadding) * 2f;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, icon.rect.width * icon.lossyScale.x / rt.lossyScale.x + pad);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, icon.rect.height * icon.lossyScale.y / rt.lossyScale.y + pad);

        frame.color = decided ? frameDecidedTint : Color.white;
        frame.raycastTarget = false;
        frame.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 指定したIndexのアイコンに、指定色のOutline(囲み枠)を表示する。
    /// Player1/Player2(またはCPU)が同じキャラを選んでいる場合は、
    /// 後から呼ばれた方の色で上書きされる(元のicon.color上書き方式と同じ挙動)。
    /// </summary>
    private void SetIconOutline(int index, Color color)
    {
        if (characterIconOutlines == null ||
            index < 0 ||
            index >= characterIconOutlines.Length ||
            characterIconOutlines[index] == null)
        {
            return;
        }

        characterIconOutlines[index].enabled = true;
        characterIconOutlines[index].effectColor = color;
    }
    private IEnumerator WaitReleaseButton()
    {
        canInput = false;

        while (true)
        {
            UpdateGamepads();

            // Padが無い場合
            if (player1Pad == null)
            {
                canInput = true;
                yield break;
            }

            // Aボタンを離したら入力開始
            if (!player1Pad.buttonSouth.isPressed)
            {
                break;
            }

            yield return null;
        }

        canInput = true;
    }
    public void Initialize()
    {
        cpuMode =
            GameModeManager.Instance.CurrentMode ==
            GameModeManager.Mode.PlayerVsCPU;

        selectState = SelectState.Player1;

        player1Decided = false;
        player2Decided = false;

        isChangingScene = false;

        player1Index = 0;
        player2Index = 0;

        previousCpuMode = cpuMode;
        canInput = true;
        SetupCharacterIcons();
        UpdateGamepads();
        UpdateSelectionColor();
    }
    private void SetupCharacterIcons()
    {
        int count = Mathf.Min(characterIcons.Length, characterIconSprites.Length);

        for (int i = 0; i < count; i++)
        {
            characterIcons[i].sprite = characterIconSprites[i];
        }
    }
    private void UpdatePreview()
    {
        // ===== PLAYER1 =====
        // 決定を待たず、カーソルが乗っているキャラの立ち絵を即座に表示する
        if (player1Index >= 0 &&
            player1Index < characterPreviewSprites.Length)
        {
            player1Preview.gameObject.SetActive(true);

            // 選択キャラクターの画像を設定
            player1Preview.sprite =
                characterPreviewSprites[player1Index];

            // 元の色で表示
            player1Preview.color = Color.white;
        }
        else
        {
            player1Preview.gameObject.SetActive(false);
        }


        // ===== PLAYER2 / CPU =====
        // Player1が決定してPlayer2/CPUの選択フェーズに入ったら、
        // 決定を待たずカーソル移動と同時に立ち絵を表示する
        bool player2SelectionStarted =
            selectState == SelectState.Player2 ||
            selectState == SelectState.CPU ||
            player2Decided ||
            (!cpuMode && player2Active);

        if (player2SelectionStarted &&
            player2Index >= 0 &&
            player2Index < characterPreviewSprites.Length)
        {
            player2Preview.gameObject.SetActive(true);

            // 選択キャラクターの画像を設定
            player2Preview.sprite =
                characterPreviewSprites[player2Index];

            // 元の色で表示
            player2Preview.color = Color.white;
        }
        else
        {
            player2Preview.gameObject.SetActive(false);
        }
    }
    private void CheckBothPlayersDecided()
    {
        // シーン移動中
        if (isChangingScene)
            return;

        // すでに確認パネル表示中
        if (isConfirming)
            return;

        // Player1とPlayer2の両方が決定した時だけ表示
        if (player1Decided && player2Decided)
        {

            ShowConfirmPanel();
        }
    }
    private IEnumerator GoToBattleRoutine()
    {
        // フェードアウト
        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.StartFadeOut();
        }

        // Battleシーンへ移動
        SceneManager.LoadScene(battleSceneName);
    }
    private void ConfirmInput()
    {
        // =========================
        // 左右で選択
        // =========================

        if (ConfirmLeft())
        {
            confirmIndex--;

            if (confirmIndex < 0)
                confirmIndex = confirmMenuTexts.Length - 1;

            PlaySe(moveSe);
            UpdateConfirmSelection();

            return;
        }

        if (ConfirmRight())
        {
            confirmIndex++;

            if (confirmIndex >= confirmMenuTexts.Length)
                confirmIndex = 0;

            PlaySe(moveSe);
            UpdateConfirmSelection();

            return;
        }

        // =========================
        // 決定
        // =========================

        if (ConfirmDecide())
        {
            PlaySe(decideSe);

            // 0 = スタート
            if (confirmIndex == 0)
            {
                StartBattle();
            }
            // 1 = 戻る
            else if (confirmIndex == 1)
            {
                CloseConfirmPanel();
            }

            return;
        }

        // =========================
        // 取消でも戻る
        // =========================

        if (ConfirmCancel())
        {
            PlaySe(cancelSe);
            CloseConfirmPanel();
        }
    }

    private void StartBattle()
    {
        if (isChangingScene)
        {
            return;
        }


        // =========================
        // 選択キャラクター保存
        // =========================

        if (!SaveSelectedCharacters())
        {
            return;
        }



        isChangingScene = true;
        isConfirming = false;


        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }


        FadeManager.Instance.FadeToScene(
            battleSceneName
        );
    }


    private void ShowConfirmPanel()
    {
        if (isConfirming)
            return;

        isConfirming = true;

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }

        if (confirmText != null)
        {
            confirmText.text = "これで戦いますか?";
        }

        // 最初は「スタート」を選択
        confirmIndex = 0;

        UpdateConfirmSelection();

    }
    private void OpenConfirmPanel()
    {
        Debug.Log("OpenConfirmPanel実行");

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
            Debug.Log("ConfirmPanelを表示しました");
        }
        else
        {
            Debug.LogError("confirmPanelがInspectorで設定されていません");
        }
    }
    private void CloseConfirmPanel()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }

        isConfirming = false;

        // Player2 / CPUを選び直す
        player2Decided = false;

        selectState = cpuMode
            ? SelectState.CPU
            : SelectState.Player1; // 対人戦はP1/P2同時選択なので状態はP1のまま

        UpdateSelectionColor();

    }
    private void UpdateConfirmSelection()
    {
        if (confirmMenuTexts == null)
            return;

        for (int i = 0; i < confirmMenuTexts.Length; i++)
        {
            if (confirmMenuTexts[i] == null)
                continue;

            if (i == confirmIndex)
            {
                confirmMenuTexts[i].color = confirmSelectColor;
            }
            else
            {
                confirmMenuTexts[i].color = confirmNormalColor;
            }
        }
    }

    /// <summary>バトル画面用の顔画像。専用の画像があればそれ、無ければアイコン画像を使う。</summary>
    private Sprite GetFaceSprite(int index)
    {
        if (index < 0) return null;

        if (characterFaceSprites != null && index < characterFaceSprites.Length && characterFaceSprites[index] != null)
            return characterFaceSprites[index];

        if (characterIconSprites != null && index < characterIconSprites.Length)
            return characterIconSprites[index];

        return null;
    }

    /// <summary>
    /// 現在選択されているキャラクターを保存する。
    /// </summary>
    private bool SaveSelectedCharacters()
    {
        if (CharacterSelectionData.Instance == null)
        {
            Debug.LogError(
                "CharacterSelectionDataがありません"
            );

            return false;
        }

        if (characterDataList == null ||
            characterDataList.Length == 0)
        {
            Debug.LogError(
                "CharacterDataListが設定されていません"
            );

            return false;
        }

        FighterCharacterData p1 =
            characterDataList[player1Index];

        FighterCharacterData p2 =
            characterDataList[player2Index];

        CharacterSelectionData.Instance
            .SetPlayer1Character(p1);

        CharacterSelectionData.Instance
            .SetPlayer2Character(p2);

        // バトルシーンのHP横の顔(FighterPortrait)用に、選ばれた顔画像も渡す
        SelectedFaceData.Set(GetFaceSprite(player1Index), GetFaceSprite(player2Index));


        return true;
    }


}