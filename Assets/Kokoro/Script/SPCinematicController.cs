using System.Collections;
using UnityEngine;

public sealed class SPCinematicController : MonoBehaviour
{
    [Header("黒演出")]
    [SerializeField]
    private SpriteRenderer blackOverlay;

    [SerializeField, Range(0f, 1f)]
    private float maxAlpha = 1f;

    [Header("構え停止時間")]
    [SerializeField, Min(0f)]
    private float stopSeconds = 0.7f;

    [Header("暗転解除時間")]
    [SerializeField, Min(0.01f)]
    private float fadeOutSeconds = 0.5f;


    private Coroutine fadeCoroutine;

    private float previousTimeScale = 1f;


    private void Awake()
    {
        SetOverlayAlpha(0f);
    }


    /// <summary>
    /// SPアニメーション開始後、
    /// 1枚目を表示してから暗転・時間停止する。
    /// </summary>
    public IEnumerator PlaySPFreeze()
    {

        // ==================================
        // ★ここが重要
        // SPAttackのアニメーションを
        // 1フレームだけ進ませる
        // ==================================
        yield return null;


        // ==================================
        // 1枚目が表示されたところで暗転
        // ==================================
        SetOverlayAlpha(maxAlpha);



        // ==================================
        // 時間停止
        // ==================================
        previousTimeScale = Time.timeScale;

        Time.timeScale = 0f;


        // TimeScale = 0でも進む
        yield return new WaitForSecondsRealtime(
            stopSeconds
        );


        // ==================================
        // 時間再開
        // ==================================
        Time.timeScale = previousTimeScale;



        // ★ここでは暗転を解除しない
        // SPAttack終了まで黒いまま
    }


    /// <summary>
    /// SPAttack終了時。
    /// 黒背景を徐々に消す。
    /// </summary>
    public void EndSPCinematic()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = previousTimeScale;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine =
            StartCoroutine(FadeOutOverlay());
    }


    private IEnumerator FadeOutOverlay()
    {
        if (blackOverlay == null)
        {
            yield break;
        }

        float startAlpha =
            blackOverlay.color.a;

        float elapsed = 0f;

        while (elapsed < fadeOutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / fadeOutSeconds
                );

            float alpha =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    t
                );

            SetOverlayAlpha(alpha);

            yield return null;
        }

        SetOverlayAlpha(0f);

        fadeCoroutine = null;
    }


    private void SetOverlayAlpha(
        float alpha
    )
    {
        if (blackOverlay == null)
        {
            return;
        }

        Color color =
            blackOverlay.color;

        color.r = 0f;
        color.g = 0f;
        color.b = 0f;
        color.a = Mathf.Clamp01(alpha);

        blackOverlay.color = color;
    }
}
