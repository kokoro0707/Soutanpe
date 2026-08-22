using UnityEngine;

/// <summary>
/// 選択されたゲームモードに応じて
/// Player2の操作方法を変更する。
/// </summary>
public sealed class BattleModeSetup : MonoBehaviour
{
    [Header("Player1")]
    [SerializeField]
    private Transform player1;

    [Header("Player2")]
    [SerializeField]
    private FighterController player2Controller;

    [SerializeField]
    private LocalFighterInputSource player2LocalInput;

    [SerializeField]
    private CPUFighterInputSource player2CPUInput;


    private void Start()
    {
        SetupBattleMode();
    }


    private void SetupBattleMode()
    {
        if (GameModeManager.Instance == null)
        {
            Debug.LogWarning(
                "GameModeManagerが無いためPvPで開始します。"
            );

            SetupPvP();
            return;
        }


        switch (GameModeManager.Instance.CurrentMode)
        {
            case GameModeManager.Mode.PlayerVsPlayer:

                SetupPvP();
                break;


            case GameModeManager.Mode.PlayerVsCPU:

                SetupCPU();
                break;
        }
    }


    private void SetupPvP()
    {
        if (player2Controller == null ||
            player2LocalInput == null)
        {
            Debug.LogError(
                "Player2のPvP設定が足りません。"
            );

            return;
        }

        player2Controller.SetInputSource(
            player2LocalInput
        );

        Debug.Log(
            "バトルモード：PLAYER VS PLAYER"
        );
    }


    private void SetupCPU()
    {
        if (player2Controller == null ||
            player2CPUInput == null)
        {
            Debug.LogError(
                "Player2のCPU設定が足りません。"
            );

            return;
        }

        player2CPUInput.SetOpponent(
            player1
        );

        player2Controller.SetInputSource(
            player2CPUInput
        );

        Debug.Log(
            "バトルモード：PLAYER VS CPU"
        );
    }
}
