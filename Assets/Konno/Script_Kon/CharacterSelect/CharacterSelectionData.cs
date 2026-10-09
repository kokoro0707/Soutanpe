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

        if (Instance != null &&
            Instance != this)
        {

            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 念のためルートへ移動
        transform.SetParent(null);

        // シーン変更しても残す
        DontDestroyOnLoad(gameObject);

    }


    public void SetPlayer1Character(
        FighterCharacterData data
    )
    {
        player1Character = data;

    }


    public void SetPlayer2Character(
        FighterCharacterData data
    )
    {
        player2Character = data;

    }
}
