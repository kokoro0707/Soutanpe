using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;

    private void Update()
    {
        Gamepad player1Pad = null;
        Gamepad player2Pad = null;

        if (Gamepad.all.Count >= 1)
            player1Pad = Gamepad.all[0];

        if (Gamepad.all.Count >= 2)
            player2Pad = Gamepad.all[1];

        string player1Status = player1Pad != null ? "Connected" : "Not Connected";
        string player2Status = player2Pad != null ? "Connected" : "Not Connected";

        debugText.text =
            $"Player1 : {player1Status}\n" +
            $"Player2 : {player2Status}\n\n" +
            $"Connected Gamepads : {Gamepad.all.Count}";
    }
}