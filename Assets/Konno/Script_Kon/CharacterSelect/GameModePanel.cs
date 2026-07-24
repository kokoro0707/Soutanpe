using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GameModePanel : MonoBehaviour
{
    [Header("選択項目")]
    [SerializeField] private TMP_Text[] menuTexts;

    [Header("カラー")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectColor = Color.red;

    [SerializeField] private GameObject characterRoot;
    private int currentIndex = 0;
    private bool decided = false;

    private void Start()
    {
        UpdateSelection();
    }

    private void Update()
    {
        if (decided)
            return;

        Gamepad pad = Gamepad.current;

        bool up = false;
        bool down = false;
        bool submit = false;

        // キーボード（デバッグ）
        if (Keyboard.current != null)
        {
            up |= Keyboard.current.upArrowKey.wasPressedThisFrame;
            down |= Keyboard.current.downArrowKey.wasPressedThisFrame;
            submit |= Keyboard.current.aKey.wasPressedThisFrame;
        }

        // コントローラー
        if (pad != null)
        {
            up |= pad.dpad.up.wasPressedThisFrame;
            down |= pad.dpad.down.wasPressedThisFrame;
            submit |= pad.buttonSouth.wasPressedThisFrame;
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
        decided = true;

        Debug.Log("GameModeManager = " + GameModeManager.Instance);
        Debug.Log("CharacterRoot = " + characterRoot);
        if (currentIndex == 0)
        {
            GameModeManager.Instance.CurrentMode =
                GameModeManager.Mode.PlayerVsPlayer;

            Debug.Log("PLAYER2モード");
        }
        else
        {
            GameModeManager.Instance.CurrentMode =
                GameModeManager.Mode.PlayerVsCPU;

            Debug.Log("CPUモード");
        }

        characterRoot.SetActive(true);
        gameObject.SetActive(false);

        
    }
}