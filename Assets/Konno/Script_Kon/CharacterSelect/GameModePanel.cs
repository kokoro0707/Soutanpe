using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameModePanel : MonoBehaviour
{
    [Header("選択項目")]
    [SerializeField] private TMP_Text[] menuTexts;

    [Header("カラー")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectColor = Color.red;
    [SerializeField] private GameObject characterRoot;
    [Header("遷移時間")]
    [SerializeField] private float panelFadeDuration = 0.2f; // ← キャラ選択⇔モード変更（今のまま変更しなくてよい想定だが手動調整可能に）

    private int currentIndex = 0;
    private bool decided = false;
    private Gamepad player1Pad;
    [SerializeField] private CharacterSelectManager characterManager;
    [SerializeField]
    private string menuSceneName = "MainMenu";

    private void OnEnable()
    {
        Initialize();
    }

    private void Update()
    {
        //Debug.Log("GameMode Update");
        if (!enabled)
            return;
        if (decided)
            return;

        if (Gamepad.all.Count > 0)
            player1Pad = Gamepad.all[0];
        else
            player1Pad = null;

        bool up = false;
        bool down = false;
        bool submit = false;

        // コントローラー
        if (player1Pad != null)
        {
             up = player1Pad.dpad.up.wasPressedThisFrame;
             down = player1Pad.dpad.down.wasPressedThisFrame;
             submit = player1Pad.buttonSouth.wasPressedThisFrame;
        }

        if (up)
        {
            currentIndex--;

            if (currentIndex < 0)
                currentIndex = menuTexts.Length - 1;

            UpdateSelection();
        }

        if (down)
        {
            currentIndex++;

            if (currentIndex >= menuTexts.Length)
                currentIndex = 0;

            UpdateSelection();
        }

        if (submit)
        {
            Decide();
        }
        // 戻る
        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            (Gamepad.current != null &&
             Gamepad.current.buttonEast.wasPressedThisFrame))
        {
            BackToMainMenu();
        }
    }
    public void BackToMainMenu()
    {
        FadeManager.Instance.FadeToScene(menuSceneName);
    }
    private void UpdateSelection()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            menuTexts[i].color =
                (i == currentIndex) ? selectColor : normalColor;
        }
    }

    private void Decide()
    {
        StartCoroutine(DecideRoutine());
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
    public void Initialize()
    {
        decided = false;
        currentIndex = 0;

        UpdateSelection();
        Debug.Log("Initialize");
    }
}