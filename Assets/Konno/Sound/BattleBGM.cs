using UnityEngine;

public class BattleBGM : MonoBehaviour
{
    [Header("バトルBGM")]
    [SerializeField] private AudioClip battleBGM;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManagerが見つかりません");
            return;
        }

        AudioManager.Instance.PlayBGM(battleBGM);

        Debug.Log("バトルBGM再生");
    }
}