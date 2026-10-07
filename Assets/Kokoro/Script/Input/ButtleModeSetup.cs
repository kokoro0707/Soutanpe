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
        // キャラクター選択結果を反映
        SetupCharacters();

        // PvP / CPUを設定
        SetupBattleMode();
    }


    /// <summary>
    /// キャラクター選択画面で選んだキャラクターを
    /// Player1 / Player2へ反映する。
    /// </summary>
    private void SetupCharacters()
    {
        if (CharacterSelectionData.Instance == null)
        {
            Debug.LogWarning(
                "CharacterSelectionDataがありません。" +
                "InspectorのCharacterDataで開始します。",
                this
            );

            return;
        }

        FighterCharacterData p1Data =
            CharacterSelectionData.Instance.Player1Character;

        FighterCharacterData p2Data =
            CharacterSelectionData.Instance.Player2Character;


        // Player1
        if (player1CharacterSetup != null &&
            p1Data != null)
        {
            player1CharacterSetup.SetCharacterData(
                p1Data
            );

            Debug.Log(
                $"Player1：{p1Data.CharacterName}を適用",
                this
            );
        }


        // Player2 / CPU
        if (player2CharacterSetup != null &&
            p2Data != null)
        {
            player2CharacterSetup.SetCharacterData(
                p2Data
            );

            Debug.Log(
                $"Player2：{p2Data.CharacterName}を適用",
                this
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
