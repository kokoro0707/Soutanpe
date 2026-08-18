using UnityEngine;

[System.Serializable]
public class CharacterData
{
    [Header("キャラクター名")]
    public string characterName;

    [Header("バトルで生成するPrefab")]
    public GameObject characterPrefab;
    public Sprite icon;
    public string sceneName;
}