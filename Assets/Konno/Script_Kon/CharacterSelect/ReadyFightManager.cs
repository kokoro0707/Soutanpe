using System.Collections;
using TMPro;
using UnityEngine;

public class ReadyFightManager : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text readyText;
    [SerializeField] private TMP_Text fightText;

    [Header("Time")]
    [SerializeField] private float readyTime = 5f;
    [SerializeField] private float fightTime = 1f;


    private IEnumerator Start()
    {
        Debug.Log("ReadyFightManager Start");

        // BattleScene開始時はゲーム停止
        Time.timeScale = 0f;

        if (readyText == null)
        {
            Debug.LogError("ReadyText が設定されていません！");
            yield break;
        }

        if (fightText == null)
        {
            Debug.LogError("FightText が設定されていません！");
            yield break;
        }

        // 最初は両方非表示
        readyText.gameObject.SetActive(false);
        fightText.gameObject.SetActive(false);

        yield return null;

        // READY表示
        Debug.Log("READY 表示");
        readyText.gameObject.SetActive(true);

        // 5秒待つ
        yield return new WaitForSecondsRealtime(readyTime);

        // READY非表示
        readyText.gameObject.SetActive(false);

        // FIGHT表示
        Debug.Log("FIGHT 表示");
        fightText.gameObject.SetActive(true);

        // 1秒待つ
        yield return new WaitForSecondsRealtime(fightTime);

        // FIGHT非表示
        fightText.gameObject.SetActive(false);

        // バトル開始
        Time.timeScale = 1f;

        Debug.Log("Battle Start!");
    }
}