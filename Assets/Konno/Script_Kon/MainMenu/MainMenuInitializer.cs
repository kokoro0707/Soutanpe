using System.Collections;
using UnityEngine;

public class MainMenuInitializer : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private float waitTime = 0f;

    private IEnumerator Start()
    {
        mainMenuRoot.SetActive(false);

        yield return null;

        mainMenuRoot.SetActive(true);

        FadeManager.Instance.StartFadeIn();
    }
}