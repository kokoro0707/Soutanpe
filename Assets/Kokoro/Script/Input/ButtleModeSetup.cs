using UnityEngine;

/// <summary>
/// 選択されたゲームモードとキャラクターを
/// Battleシーンへ反映する。
/// </summary>
public sealed class BattleModeSetup : MonoBehaviour
{
    [Header("Player1")]
    [SerializeField]
    private Transform player1;

    [SerializeField]
    private FighterCharacterSetup player1CharacterSetup;


    [Header("Player2")]
    [SerializeField]
    private FighterController player2Controller;

    [SerializeField]
    private LocalFighterInputSource player2LocalInput;

    [SerializeField]
    private CPUFighterInputSource player2CPUInput;

    [SerializeField]
    private FighterCharacterSetup player2CharacterSetup;


    private void Start()
    {

        SetupCharacters();
        SetupBattleMode();
    }


    private void SetupCharacters()
    {

        // 選択データがバトルシーンまで残っているか
        if (CharacterSelectionData.Instance == null)
        {
            Debug.LogError(
                "【ERROR】CharacterSelectionData.Instance がありません"
            );

            return;
        }

        FighterCharacterData p1Data =
            CharacterSelectionData.Instance.Player1Character;

        FighterCharacterData p2Data =
            CharacterSelectionData.Instance.Player2Character;



        // =========================
        // P1
        // =========================

        if (player1CharacterSetup == null)
        {
            Debug.LogError(
                "【ERROR】Player1 CharacterSetup が未設定"
            );
        }
        else if (p1Data == null)
        {
            Debug.LogError(
                "【ERROR】P1のCharacterDataがNULL"
            );
        }
        else
        {

            player1CharacterSetup.SetCharacterData(
                p1Data
            );
        }


        // =========================
        // P2
        // =========================

        if (player2CharacterSetup == null)
        {
            Debug.LogError(
                "【ERROR】Player2 CharacterSetup が未設定"
            );
        }
        else if (p2Data == null)
        {
            Debug.LogError(
                "【ERROR】P2のCharacterDataがNULL"
            );
        }
        else
        {

            player2CharacterSetup.SetCharacterData(
                p2Data
            );
        }
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

    }
}
