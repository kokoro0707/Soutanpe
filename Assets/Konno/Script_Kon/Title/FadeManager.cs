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
    [SerializeField] private float fadeOutTime = 1f;       // パネル切替など汎用
    [SerializeField] private float fadeInTime = 2f;


    private bool isFading = false;
    private bool firstScene = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Color c = fadeImage.color;
            if (gameObject.scene.name == "Title")
                c.a = 0f;   // タイトルは最初から表示
            else
                c.a = 1f;   // それ以外は黒
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        transform.SetAsLastSibling();
    }
    //----------------------------------------------------
    // シーン切替
    //----------------------------------------------------

    public void FadeToScene(string sceneName)
    {
        if (isFading)
            return;

        StartCoroutine(FadeScene(sceneName));
    }

    private IEnumerator FadeScene(string sceneName)
    {
        isFading = true;

        // フェードアウト
        yield return StartCoroutine(FadeOut(sceneFadeOutTime));

        // 黒を維持
        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;

        // シーン切替
        SceneManager.LoadScene(sceneName);

        // MainMenuのStart()やAwake()が終わるまで待つ
        yield return null;
        yield return null;

        // 強制的にフェードイン
        yield return StartCoroutine(FadeIn(sceneFadeInTime));

        isFading = false;
        // 最初のシーンではない
        firstScene = false;
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