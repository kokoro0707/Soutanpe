using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("キャラクターアイコン")]
    [SerializeField] private Image[] characterIcons;

    [Header("カラー設定")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color player1Color = Color.red;
    [SerializeField] private Color player2Color = Color.blue;
    [SerializeField] private Color decidedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private int player1Index = 0;
    private int player2Index = 1;

    private bool player1Decided;
    private bool player2Decided;

    private bool player2Active;

    private Gamepad player1Pad;
    private Gamepad player2Pad;

    private void Start()
    {
        UpdateGamepads();
        UpdateSelectionColor();
    }
    private void Update()
    {
        Debug.Log("Gamepad Count : " + Gamepad.all.Count);

        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            Debug.Log($"[{i}] {Gamepad.all[i].displayName}");
        }

        UpdateGamepads();

        if (player1Pad != null)
            Player1Input();

        if (player2Active)
            Player2Input();
    }

    private void UpdateGamepads()
    {
        player1Pad = null;
        player2Pad = null;

        if (Gamepad.all.Count >= 1)
            player1Pad = Gamepad.all[0];

        if (Gamepad.all.Count >= 2)
            player2Pad = Gamepad.all[1];

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

    private void Player1Input()
    {
        if (player1Decided)
            return;

        if (player1Pad.dpad.left.wasPressedThisFrame)
        {
            player1Index--;

            if (player1Index < 0)
                player1Index = characterIcons.Length - 1;

            UpdateSelectionColor();
        }

        if (player1Pad.dpad.right.wasPressedThisFrame)
        {
            player1Index++;

            if (player1Index >= characterIcons.Length)
                player1Index = 0;

            UpdateSelectionColor();
        }

        if (player1Pad.buttonSouth.wasPressedThisFrame)
        {
            player1Decided = true;
            UpdateSelectionColor();
        }
    }

    private void Player2Input()
    {
        if (player2Decided)
            return;

        if (player2Pad.dpad.left.wasPressedThisFrame)
        {
            player2Index--;

            if (player2Index < 0)
                player2Index = characterIcons.Length - 1;

            UpdateSelectionColor();
        }

        if (player2Pad.dpad.right.wasPressedThisFrame)
        {
            player2Index++;

            if (player2Index >= characterIcons.Length)
                player2Index = 0;

            UpdateSelectionColor();
        }

        if (player2Pad.buttonSouth.wasPressedThisFrame)
        {
            player2Decided = true;
            UpdateSelectionColor();
        }
    }

    private void UpdateSelectionColor()
    {
        // 全員白
        foreach (Image icon in characterIcons)
        {
            icon.color = normalColor;
        }

        // 1P
        if (player1Decided)
            characterIcons[player1Index].color = decidedColor;
        else
            characterIcons[player1Index].color = player1Color;

        // 2P
        if (player2Active)
        {
            if (player2Decided)
                characterIcons[player2Index].color = decidedColor;
            else
                characterIcons[player2Index].color = player2Color;
        }
    }
}