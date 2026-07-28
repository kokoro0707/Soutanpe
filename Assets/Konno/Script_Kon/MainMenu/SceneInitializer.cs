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
    // まず表示
    if (sceneRoot != null)
        sceneRoot.SetActive(true);

    // 1フレーム待ってUIを描画させる
    yield return null;

    // その後フェードイン
    FadeManager.Instance.StartFadeIn();
    }
}