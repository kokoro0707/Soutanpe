using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeOutTime = 2f;
    [SerializeField] private float fadeInTime = 5f;

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

    // タイトルなどから呼ぶ
    public void FadeToScene(string sceneName)
    {
        StartCoroutine(Fade(sceneName));
    }

    // フェードアウトしてシーン切替
    private IEnumerator Fade(string sceneName)
    {
        float t = 0;

        while (t < fadeOutTime)
        {
            t += Time.deltaTime;

            Color c = fadeImage.color;
            c.a = Mathf.Lerp(0f, 1f, t / fadeOutTime);
            fadeImage.color = c;

            yield return null;
        }

        // 完全に黒にする
        Color black = fadeImage.color;
        black.a = 1f;
        fadeImage.color = black;

        // シーン切替
        yield return SceneManager.LoadSceneAsync(sceneName);

        // ここではフェードインしない
        // MainMenuInitializerから StartFadeIn() を呼ぶ
    }

    // MainMenuから呼ぶ
    public Coroutine StartFadeIn()
    {
        StopAllCoroutines();
        return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float t = 0;

        while (t < fadeInTime)
        {
            t += Time.deltaTime;

            Color c = fadeImage.color;
            c.a = Mathf.Lerp(1f, 0f, t / fadeInTime);
            fadeImage.color = c;

            yield return null;
        }

        Color clear = fadeImage.color;
        clear.a = 0f;
        fadeImage.color = clear;
    }
    public Coroutine StartFadeOut()
    {
        StopAllCoroutines();
        return StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float t = 0;

        while (t < fadeOutTime)
        {
            t += Time.deltaTime;

            Color c = fadeImage.color;
            c.a = Mathf.Lerp(0f, 1f, t / fadeOutTime);
            fadeImage.color = c;

            yield return null;
        }

        // 完全に黒
        Color black = fadeImage.color;
        black.a = 1f;
        fadeImage.color = black;
    }
}