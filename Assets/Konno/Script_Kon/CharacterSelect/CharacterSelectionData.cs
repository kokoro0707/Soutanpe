using UnityEngine;

/// <summary>
/// キャラクター選択画面で選択したキャラクターを
/// Battleシーンまで保持する。
/// </summary>
public sealed class CharacterSelectionData : MonoBehaviour
{
    public static CharacterSelectionData Instance
    {
        get;
        private set;
    }


    [Header("Player1")]
    [SerializeField]
    private FighterCharacterData player1Character;


    [Header("Player2 / CPU")]
    [SerializeField]
    private FighterCharacterData player2Character;


    public FighterCharacterData Player1Character =>
        player1Character;

    public FighterCharacterData Player2Character =>
        player2Character;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    public void SetPlayer1Character(
        FighterCharacterData data
    )
    {
        player1Character = data;

        Debug.Log(
            $"P1選択：" +
            $"{(data != null ? data.CharacterName : "NULL")}",
            this
        );
    }


    public void SetPlayer2Character(
        FighterCharacterData data
    )
    {
        player2Character = data;

        Debug.Log(
            $"P2選択：" +
            $"{(data != null ? data.CharacterName : "NULL")}",
            this
        );
    }
}
