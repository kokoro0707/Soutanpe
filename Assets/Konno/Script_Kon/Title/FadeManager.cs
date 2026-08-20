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
    [SerializeField] private float sceneFadeOutTime = 0.5f;
    [SerializeField] private float sceneFadeInTime = 0.5f;

    [SerializeField] private float fadeOutTime = 1f;
    [SerializeField] private float fadeInTime = 2f;

    private bool isFading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ゲーム開始時だけ透明
            SetFadeAlpha(0f);
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

    //==================================================
    // シーン遷移
    //==================================================

    public void FadeToScene(string sceneName)
    {
        if (isFading)
            return;

        StartCoroutine(FadeScene(sceneName));
    }

    private IEnumerator FadeScene(string sceneName)
    {
        isFading = true;

        Debug.Log("FadeScene Start");

        // フェードアウト
        yield return StartCoroutine(FadeOut(sceneFadeOutTime));

        Debug.Log("FadeOut Complete");

        // 完全な黒を維持
        SetFadeAlpha(1f);

        // シーン切り替え
        Debug.Log("Load Scene : " + sceneName);
        SceneManager.LoadScene(sceneName);

        yield return null;
        yield return null;

        Debug.Log("FadeIn Start");

        // フェードイン
        yield return StartCoroutine(FadeIn(sceneFadeInTime));

        Debug.Log("FadeIn Complete");

        isFading = false;
    }

    //==================================================
    // 通常フェードアウト
    //==================================================

    public Coroutine StartFadeOut()
    {
        return StartCoroutine(FadeOut(fadeOutTime));
    }

    public Coroutine StartFadeOut(float time)
    {
        return StartCoroutine(FadeOut(time));
    }

    private IEnumerator FadeOut(float time)
    {
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;

            float alpha = Mathf.Lerp(0f, 1f, t / time);
            SetFadeAlpha(alpha);

            yield return null;
        }

        SetFadeAlpha(1f);
    }

    //==================================================
    // 通常フェードイン
    //==================================================

    public Coroutine StartFadeIn()
    {
        return StartCoroutine(FadeIn(fadeInTime));
    }

    public Coroutine StartFadeIn(float time)
    {
        return StartCoroutine(FadeIn(time));
    }

    private IEnumerator FadeIn(float time)
    {
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, t / time);
            SetFadeAlpha(alpha);

            yield return null;
        }

        SetFadeAlpha(0f);
    }

    //==================================================
    // Alpha変更
    //==================================================

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }
}