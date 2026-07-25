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
    private Gamepad player1Pad;
    [SerializeField] private CharacterSelectManager characterManager;

    private void Start()
    {
        UpdateSelection();
    }

    private void Update()
    {
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

        GameModeManager.Instance.CurrentMode =
            (currentIndex == 0) ? GameModeManager.Mode.PlayerVsPlayer
                                 : GameModeManager.Mode.PlayerVsCPU;

        characterRoot.SetActive(true);
        characterManager.Initialize();   // OnEnableと重複するならOnEnable側を削除するか統一する
        gameObject.SetActive(false);
    }

    private IEnumerator OpenCharacterRoot()
    {
        while (player1Pad != null && player1Pad.buttonSouth.isPressed)
        {
            yield return null;
        }

        characterRoot.SetActive(true);
        gameObject.SetActive(false);
    }
}