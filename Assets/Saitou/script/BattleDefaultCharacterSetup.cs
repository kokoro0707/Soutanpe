
using UnityEngine;

/// <summary>
/// キャラクター選択画面を経由せずに、
/// バトル開始時に使用キャラクターを自動設定する。
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class BattleDefaultCharacterSetup : MonoBehaviour
{
    [Header("Player1 の設定")]
    [SerializeField]
    private FighterCharacterSetup player1Setup;

    [SerializeField]
    private FighterCharacterData player1Character;

    [Header("Player2 の設定")]
    [SerializeField]
    private FighterCharacterSetup player2Setup;

    [SerializeField]
    private FighterCharacterData player2Character;

    private void Start()
    {
        // Player1 のキャラクターを設定
        if (player1Setup != null && player1Character != null)
        {
            player1Setup.SetCharacterData(player1Character);
            Debug.Log(
                $"Player1 のキャラクターを設定しました: {player1Character.CharacterName}"
            );
        }
        else
        {
            Debug.LogError(
                "Player1 の設定が不足しています。Inspector を確認してください。"
            );
        }

        // Player2 のキャラクターを設定
        if (player2Setup != null && player2Character != null)
        {
            player2Setup.SetCharacterData(player2Character);
            Debug.Log(
                $"Player2 のキャラクターを設定しました: {player2Character.CharacterName}"
            );
        }
        else
        {
            Debug.LogError(
                "Player2 の設定が不足しています。Inspector を確認してください。"
            );
        }
    }
}