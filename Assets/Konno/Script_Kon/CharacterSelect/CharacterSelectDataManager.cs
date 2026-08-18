using UnityEngine;

public class CharacterSelectDataManager : MonoBehaviour
{
    public static CharacterSelectDataManager Instance;

    [Header("Player1の選択キャラクター")]
    public CharacterData player1Character;

    [Header("Player2 / CPUの選択キャラクター")]
    public CharacterData player2Character;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}