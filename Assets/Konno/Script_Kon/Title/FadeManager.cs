using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// シーンをまたいで使う暗転(フェード)管理。
///
/// シーンに置いた FadeManager はどこに置いてあっても(Canvasの子でもOK)、
/// 最初の1つが起動した時に「専用の常駐オブジェクト(FadeManager (Persistent))」を
/// 自動で作り、以降はそちらがフェードを担当する。
/// シーンに置いた側の FadeManager と黒画像は、自動で無効化される(GameObjectは消さない)。
///  → 他のスクリプトを巻き込んで消したり、
///    「DontDestroyOnLoad only works for root GameObjects」で常駐できない問題が起きない。
/// </summary>
public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    [Header("Fade Image(色の指定に使う。実際の暗転は自動生成の画像で行う)")]
    [SerializeField] private Image fadeImage;

    [Header("Fade Time")]
    [SerializeField] private float sceneFadeOutTime = 0.5f;
    [SerializeField] private float sceneFadeInTime = 0.5f;

    [SerializeField] private float fadeOutTime = 1f;
    [SerializeField] private float fadeInTime = 2f;

    [Header("表示順(大きいほど手前)")]
    [SerializeField] private int sortingOrder = 5000;

    private bool isFading = false;

    /// <summary>シーン遷移のフェード中かどうか</summary>
    public bool IsFading => isFading;

    private static bool creatingPersistent;

    private void Awake()
    {
        // 常駐用として生成中のもの(下の AddComponent)は、ここでは何もしない
        if (creatingPersistent) return;

        if (Instance != null && Instance != this)
        {
            // 2つ目以降(シーンに置いてある分)は、自分と黒画像だけ無効化する
            DisableSceneCopy();
            return;
        }

        // ---- 最初の1つ:常駐用オブジェクトを作って役目を引き継ぐ ----
        Color color = fadeImage != null ? fadeImage.color : Color.black;
        color.a = 0f;

        creatingPersistent = true;
        var go = new GameObject("FadeManager (Persistent)");
        var fm = go.AddComponent<FadeManager>();
        creatingPersistent = false;

        fm.sceneFadeOutTime = sceneFadeOutTime;
        fm.sceneFadeInTime = sceneFadeInTime;
        fm.fadeOutTime = fadeOutTime;
        fm.fadeInTime = fadeInTime;
        fm.sortingOrder = sortingOrder;
        fm.BuildOverlay(color);

        DontDestroyOnLoad(go); // ルートなので確実に常駐する
        Instance = fm;

        DisableSceneCopy();
    }

    private void DisableSceneCopy()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
            fadeImage.enabled = false;
        }
        Destroy(this); // コンポーネントだけ消す(GameObjectや他のスクリプトは残す)
    }

    private void BuildOverlay(Color color)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var imgGO = new GameObject("FadeImage", typeof(RectTransform), typeof(Image));
        imgGO.transform.SetParent(transform, false);
        var rt = (RectTransform)imgGO.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage = imgGO.GetComponent<Image>();
        fadeImage.color = color;
        SetFadeAlpha(0f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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

        // フェードアウト
        yield return StartCoroutine(FadeOut(sceneFadeOutTime));

        // 完全な黒を維持
        SetFadeAlpha(1f);

        // 前のシーンで止めた時間を戻しておく
        Time.timeScale = 1f;

        // シーン切り替え
        Debug.Log("Load Scene : " + sceneName);
        SceneManager.LoadScene(sceneName);

        // 新しいシーンの生成を待つ
        yield return null;
        yield return null;

        // Time.timeScale = 0でも動く
        yield return StartCoroutine(FadeIn(sceneFadeInTime));

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
            t += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(0f, 1f, t / time));
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
            t += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(1f, 0f, t / time));
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

        // 透明な時はクリック等を吸い取らない
        fadeImage.raycastTarget = alpha > 0.01f;
    }
}