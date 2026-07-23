using System.Collections;
using UnityEngine;

public class MainMenuInitializer : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private float waitTime = 0.8f;

    private IEnumerator Start()
    {
        // メニューは非表示
        mainMenuRoot.SetActive(false);

        // 黒画面のまま待つ
        yield return new WaitForSeconds(waitTime);

        // メニュー表示
        mainMenuRoot.SetActive(true);

        // フェードイン
        FadeManager.Instance.StartFadeIn();
    }
}