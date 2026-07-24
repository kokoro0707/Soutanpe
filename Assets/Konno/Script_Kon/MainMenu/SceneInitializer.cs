using System.Collections;
using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    [Header("シーン表示の親オブジェクト")]
    [SerializeField] private GameObject sceneRoot;

    [Header("黒画面維持時間")]
    [SerializeField] private float waitTime = 0.5f;

    private IEnumerator Start()
    {
        if (sceneRoot != null)
            sceneRoot.SetActive(false);

        // 黒画面のまま待機
        yield return new WaitForSeconds(waitTime);

        if (sceneRoot != null)
            sceneRoot.SetActive(true);

        // フェードイン
        if (FadeManager.Instance != null)
            FadeManager.Instance.StartFadeIn();
    }
}