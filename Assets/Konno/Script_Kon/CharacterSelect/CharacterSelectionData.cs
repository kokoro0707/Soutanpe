using UnityEngine;

public sealed class CharacterSelectionData : MonoBehaviour
{
    public static CharacterSelectionData Instance { get; private set; }

    [SerializeField]
    private FighterCharacterData player1Character;

    [SerializeField]
    private FighterCharacterData player2Character;


    public FighterCharacterData Player1Character
        => player1Character;

    public FighterCharacterData Player2Character
        => player2Character;


    private void Awake()
    {
        Debug.Log("yCharacterSelectionDatazAwake");

        if (Instance != null &&
            Instance != this)
        {
            Debug.Log(
                "yCharacterSelectionDatazd•¡‚µ‚½‚Ì‚Åíœ"
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        // ”O‚Ì‚½‚ßƒ‹[ƒg‚ÖˆÚ“®
        transform.SetParent(null);

        // ƒV[ƒ“•ÏX‚µ‚Ä‚àc‚·
        DontDestroyOnLoad(gameObject);

        Debug.Log(
            "yCharacterSelectionDatazDontDestroyOnLoadİ’èŠ®—¹"
        );
    }


    public void SetPlayer1Character(
        FighterCharacterData data
    )
    {
        player1Character = data;

        Debug.Log(
            $"y‘I‘ğ•Û‘¶zP1 = " +
            $"{(data != null ? data.name : "NULL")}"
        );
    }


    public void SetPlayer2Character(
        FighterCharacterData data
    )
    {
        player2Character = data;

        Debug.Log(
            $"y‘I‘ğ•Û‘¶zP2 = " +
            $"{(data != null ? data.name : "NULL")}"
        );
    }
}
