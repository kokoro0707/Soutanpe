using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

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
    private bool cpuMode;
    private bool selectingCPU;
    private bool canInput = false;

    private void OnEnable()
    {
        cpuMode = GameModeManager.Instance.CurrentMode ==
                  GameModeManager.Mode.PlayerVsCPU;

        UpdateGamepads();
        UpdateSelectionColor();

        StartCoroutine(EnableInputNextFrame());
    }
    private void Update()
    {
        //Debug.Log("CharacterSelectManager Update");
        if (!canInput)
            return;
        //Debug.Log("Gamepad Count : " + Gamepad.all.Count);

        //for (int i = 0; i < Gamepad.all.Count; i++)
        //{
        //    Debug.Log($"[{i}] {Gamepad.all[i].displayName}");
        //}

        UpdateGamepads();

        //Debug.Log(player1Pad);
        if (player1Pad != null)
            Player1Input();

        if (cpuMode)
        {
            CPUInput();
        }
        else if (player2Active)
        {
            Player2Input();
        }
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
        Debug.Log(player1Pad.dpad.ReadValue());
 
        if (player1Decided)
            return;

        if (player1Pad.dpad.left.isPressed)
        {
            Debug.Log("P1 LEFT");
            player1Index--;

            if (player1Index < 0)
                player1Index = characterIcons.Length - 1;

            UpdateSelectionColor();
        }

        if (player1Pad.dpad.right.isPressed)
        {
            Debug.Log("P1 RIGHT");
            player1Index++;

            if (player1Index >= characterIcons.Length)
                player1Index = 0;

            UpdateSelectionColor();
        }

        if (player1Pad.buttonSouth.wasPressedThisFrame)
        {
            Debug.Log("P1 A");
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
    private void CPUInput()
    {
        // まだ1Pが決定していないなら何もしない
        if (!player1Decided)
            return;

        // 1P決定後にCPU選択開始
        selectingCPU = true;

        if (player1Pad.dpad.left.wasPressedThisFrame)
        {
            player2Index--;

            if (player2Index < 0)
                player2Index = characterIcons.Length - 1;

            UpdateSelectionColor();
        }

        if (player1Pad.dpad.right.wasPressedThisFrame)
        {
            player2Index++;

            if (player2Index >= characterIcons.Length)
                player2Index = 0;

            UpdateSelectionColor();
        }

        if (player1Pad.buttonSouth.wasPressedThisFrame)
        {
            player2Decided = true;

            Debug.Log("CPUキャラクター決定");

            UpdateSelectionColor();

            // TODO : バトルシーンへ
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

        // 2PまたはCPU
        if (cpuMode)
        {
            if (selectingCPU)
            {
                if (player2Decided)
                    characterIcons[player2Index].color = decidedColor;
                else
                    characterIcons[player2Index].color = Color.yellow;
            }
        }
        else if (player2Active)
        {
            if (player2Decided)
                characterIcons[player2Index].color = decidedColor;
            else
                characterIcons[player2Index].color = player2Color;
        }
    }
    private IEnumerator EnableInputNextFrame()
    {
        canInput = false;

        yield return null;   // 1フレーム待つ

        canInput = true;
    }
}