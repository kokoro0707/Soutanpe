using UnityEngine;

public class CharacterSelectionData : MonoBehaviour
{
    public static CharacterSelectionData Instance;

    [Header("Player1が選択したキャラクター")]
    public GameObject player1Character;

    [Header("Player2が選択したキャラクター")]
    public GameObject player2Character;

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