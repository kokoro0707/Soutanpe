using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    [Header("Fade Image")]
    [SerializeField] private Image fadeImage;

    [Header("Fade Time")]
    [SerializeField] private float sceneFadeOutTime = 0.5f; // シーン遷移専用
    [SerializeField] private float sceneFadeInTime = 0.5f;
    [SerializeField] private float fadeOutTime = 0.2f;       // パネル切替など汎用
    [SerializeField] private float fadeInTime = 0.2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 最初は透明にしておく
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //----------------------------------------------------
    // シーン切替
    //----------------------------------------------------

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeScene(sceneName));
    }

    private IEnumerator FadeScene(string sceneName)
    {
        // フェードアウト
        yield return FadeOut(sceneFadeOutTime);

        // シーン切替
        yield return SceneManager.LoadSceneAsync(sceneName);

        // 次のシーン側(Startなど)でフェードインする
    }

    //----------------------------------------------------
    // フェードアウト
    //----------------------------------------------------

    public Coroutine StartFadeOut()
    {
        StopAllCoroutines();
        return StartCoroutine(FadeOut(fadeOutTime));
    }

    public Coroutine StartFadeOut(float time)
    {
        StopAllCoroutines();
        return StartCoroutine(FadeOut(time));
    }

    private IEnumerator FadeOut(float time)
    {
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;

            Color c = fadeImage.color;
            c.a = Mathf.Lerp(0f, 1f, t / time);
            fadeImage.color = c;

            yield return null;
        }

        Color c2 = fadeImage.color;
        c2.a = 1f;
        fadeImage.color = c2;
    }

    //----------------------------------------------------
    // フェードイン
    //----------------------------------------------------

    public Coroutine StartFadeIn()
    {
        StopAllCoroutines();
        return StartCoroutine(FadeIn(fadeInTime));
    }

    public Coroutine StartFadeIn(float time)
    {
        StopAllCoroutines();
        return StartCoroutine(FadeIn(time));
    }

    private IEnumerator FadeIn(float time)
    {
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;

            Color c = fadeImage.color;
            c.a = Mathf.Lerp(1f, 0f, t / time);
            fadeImage.color = c;

            yield return null;
        }

        Color c2 = fadeImage.color;
        c2.a = 0f;
        fadeImage.color = c2;
    }
}