using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;
using PersonaMenuUI;

public class GameModePanel : MonoBehaviour
{
    [Header("選択項目")]
    [SerializeField] private TMP_Text[] menuTexts;

    [Header("カラー")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectColor = Color.red;
    [SerializeField] private GameObject characterRoot;
    [Header("遷移時間")]
    [SerializeField] private float panelFadeDuration = 0.2f; // ← キャラ選択⇔モード変更(今のまま変更しなくてよい想定だが手動調整可能に)

    [Header("SE")]
    [SerializeField] private AudioClip moveSe;   // カーソル移動音
    [SerializeField] private AudioClip decideSe; // 決定音
    [SerializeField] private AudioClip cancelSe; // 戻る音(MainMenuへ)

    [Header("斜めカーソル")]
    [Tooltip("SlantedRectで作った斜めカーソルを制御するコンポーネント。未設定でも動作する(その場合はカーソル演出なし)。")]
    [SerializeField] private MenuCursorSelector cursor;

    [Header("キーボード操作")]
    [Tooltip("移動キー(上)")]
    [SerializeField] private Key keyboardUpKey = Key.UpArrow;
    [Tooltip("移動キー(下)")]
    [SerializeField] private Key keyboardDownKey = Key.DownArrow;
    [Tooltip("決定キー(複数指定可)")]
    [SerializeField] private Key[] keyboardDecideKeys = { Key.A, Key.Enter, Key.Space };

    [System.Serializable]
    public class ItemSceneLink
    {
        [Tooltip("Inspector上で分かりやすくするためのメモ(処理には使わない)")]
        public string label;

        [Tooltip("この項目を決定した時に遷移するシーン名(Build Settingsに登録済みのもの)。" +
                 "空欄なら従来どおり(キャラクター選択パネルへ進む)")]
        public string sceneName;

        [Tooltip("ONにすると、シーン遷移の前にゲームモードをModeに設定する")]
        public bool setGameMode;

        [Tooltip("Set Game ModeがONの時に設定するモード")]
        public GameModeManager.Mode mode = GameModeManager.Mode.PlayerVsPlayer;
    }

    [Header("項目ごとのシーン遷移 (任意)")]
    [Tooltip("Menu Textsと同じ順番で設定する(要素0=1つ目の項目)。" +
             "Scene Nameを入れた項目を決定すると、キャラ選択パネルではなく、そのシーンへフェードして遷移する。" +
             "要素数が足りない/Scene Nameが空の項目は、従来どおりの動作(0=Player vs Player、それ以外=Player vs CPU)")]
    [SerializeField] private ItemSceneLink[] itemSceneLinks;

    private int currentIndex = 0;
    private bool decided = false;
    private Gamepad player1Pad;
    [SerializeField] private CharacterSelectManager characterManager;
    [SerializeField]
    private string menuSceneName = "MainMenu";
    private bool changingScene = false;

    public void BackToMainMenu()
    {
        if (changingScene)
            return;

        changingScene = true;
        decided = true;

        Debug.Log("モード選択 → MainMenu");

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeToScene(menuSceneName);
        }
        else
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }

    private void OnEnable()
    {
        changingScene = false;
        Initialize();
    }

    private void Update()
    {
        //if (player1Pad != null)
        //{
        //    Debug.Log(player1Pad.buttonEast.wasPressedThisFrame);
        //}
        if (!enabled || changingScene)
            return;

        player1Pad = Gamepad.current;

        // 戻る(キーボードはEscape、ゲームパッドはBボタン)
        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            (player1Pad != null &&
                player1Pad.buttonEast.wasPressedThisFrame))
        {
            Debug.Log("GameModePanel : B");
            PlaySe(cancelSe);
            BackToMainMenu();
            return;
        }

        if (decided)
            return;

        bool up = IsUpPressed();
        bool down = IsDownPressed();
        bool submit = IsDecidePressed();

        if (up)
        {
            currentIndex--;

            if (currentIndex < 0)
                currentIndex = menuTexts.Length - 1;

            PlaySe(moveSe);
            UpdateSelection();
        }

        if (down)
        {
            currentIndex++;

            if (currentIndex >= menuTexts.Length)
                currentIndex = 0;

            PlaySe(moveSe);
            UpdateSelection();
        }

        if (submit)
        {
            PlaySe(decideSe);
            Decide();
        }
    }

    private bool IsUpPressed()
    {
        bool key = Keyboard.current != null && Keyboard.current[keyboardUpKey].wasPressedThisFrame;
        bool gp = player1Pad != null && player1Pad.dpad.up.wasPressedThisFrame;
        return key || gp;
    }

    private bool IsDownPressed()
    {
        bool key = Keyboard.current != null && Keyboard.current[keyboardDownKey].wasPressedThisFrame;
        bool gp = player1Pad != null && player1Pad.dpad.down.wasPressedThisFrame;
        return key || gp;
    }

    private bool IsDecidePressed()
    {
        bool gp = player1Pad != null && player1Pad.buttonSouth.wasPressedThisFrame;
        return IsKeyboardDecidePressed() || gp;
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

    private void PlaySe(AudioClip clip)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            menuTexts[i].color =
                (i == currentIndex) ? selectColor : normalColor;
        }

        // 斜めカーソルを現在の選択位置へ移動させる。
        // MenuCursorSelector側の Use Internal Input / Follow Event System Selection は
        // OFFにしておき、選択の主導権はこのGameModePanelが持つ。
        if (cursor != null) cursor.Select(currentIndex);
    }

    private void Decide()
    {
        // この項目にシーン遷移が設定されていれば、そのシーンへ移動する
        ItemSceneLink link = GetSceneLink(currentIndex);
        Debug.Log(
            $"[GameModePanel] 決定: index={currentIndex}, " +
            $"シーン設定={(link != null ? link.sceneName : "なし(キャラ選択へ進みます)")}, " +
            $"Item Scene Links の要素数={(itemSceneLinks == null ? 0 : itemSceneLinks.Length)}", this);

        if (link != null)
        {
            LoadLinkedScene(link);
            return;
        }

        StartCoroutine(DecideRoutine());
    }

    private ItemSceneLink GetSceneLink(int index)
    {
        if (itemSceneLinks == null || index < 0 || index >= itemSceneLinks.Length) return null;

        ItemSceneLink link = itemSceneLinks[index];
        if (link == null || string.IsNullOrWhiteSpace(link.sceneName)) return null;

        return link;
    }

    private void LoadLinkedScene(ItemSceneLink link)
    {
        if (changingScene) return;

        // Build Settingsに無いシーン名だと、遷移できずに止まる。原因が分かるよう先に確認する
        if (!Application.CanStreamedLevelBeLoaded(link.sceneName))
        {
            Debug.LogError(
                $"[GameModePanel] シーン '{link.sceneName}' を読み込めません。" +
                "Build Settings(File > Build Profiles > Scene List)にシーンが登録されているか、" +
                "Scene Nameの綴り(大文字小文字も)が正しいか確認してください。", this);
            return;
        }

        changingScene = true;
        decided = true;

        if (link.setGameMode && GameModeManager.Instance != null)
        {
            GameModeManager.Instance.CurrentMode = link.mode;
        }

        Debug.Log($"モード選択 → シーン遷移: {link.sceneName}");

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeToScene(link.sceneName);
        }
        else
        {
            SceneManager.LoadScene(link.sceneName);
        }
    }
    private IEnumerator DecideRoutine()
    {
        decided = true;

        GameModeManager.Instance.CurrentMode =
            (currentIndex == 0)
            ? GameModeManager.Mode.PlayerVsPlayer
            : GameModeManager.Mode.PlayerVsCPU;

        // フェードアウト
        yield return FadeManager.Instance.StartFadeOut(panelFadeDuration);

        // 初期化
        characterManager.Initialize();

        // パネル切り替え
        characterRoot.SetActive(true);
        gameObject.SetActive(false);

        // フェードイン
        FadeManager.Instance.StartFadeIn(panelFadeDuration);
    }
    private IEnumerator BackRoutine()
    {
        // フェードアウト
        yield return FadeManager.Instance.StartFadeOut(0.5f);

        // メインメニューへ
        SceneManager.LoadScene(menuSceneName);
    }
    public void Initialize()
    {
        decided = false;
        changingScene = false;
        currentIndex = 0;

        // 画面を開き直した時、カーソルの途中状態や前回の選択位置を残さず先頭へ合わせる
        if (cursor != null) cursor.SetIndexImmediate(0);

        UpdateSelection();

#if UNITY_EDITOR
        Debug.Log("GameModePanel Initialize");
#endif
    }
}