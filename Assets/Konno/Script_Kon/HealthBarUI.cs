using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private HealthSystem target;
    [SerializeField] private Image frontBar;   // 即時反映される本体バー
    [SerializeField] private Image delayBar;   // 遅れて減るトレイルバー(ダメージ表示)

    [Header("演出設定")]
    [SerializeField] private float delayBeforeDrain = 0.4f;
    [SerializeField] private float drainSpeed = 0.6f; // 1秒あたりに減る割合(0から1)

    [Header("ピンチ時の色変化(任意)")]
    [SerializeField] private bool changeColorWhenLow = true;
    [SerializeField] private float lowHpThreshold = 0.25f;
    [SerializeField] private Color normalColor = new Color(0.95f, 0.85f, 0.15f); // 黄色
    [SerializeField] private Color lowHpColor = new Color(0.9f, 0.2f, 0.2f);     // 赤

    private Coroutine drainRoutine;

    private void OnEnable()
    {
        if (target == null) return;

        target.OnHealthChanged += HandleHealthChanged;
        SetImmediate((float)target.CurrentHP / target.MaxHP);
    }

    private void OnDisable()
    {
        if (target != null) target.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;

        // 本体バーは即座に反映
        if (frontBar != null)
        {
            frontBar.fillAmount = ratio;

            if (changeColorWhenLow)
                frontBar.color = ratio <= lowHpThreshold ? lowHpColor : normalColor;
        }

        // トレイルバーは少し待ってからゆっくり追従させ、ダメージ量を見せる
        if (drainRoutine != null) StopCoroutine(drainRoutine);
        drainRoutine = StartCoroutine(DrainDelayBar(ratio));
    }

    private IEnumerator DrainDelayBar(float targetRatio)
    {
        yield return new WaitForSeconds(delayBeforeDrain);

        while (delayBar != null && delayBar.fillAmount > targetRatio)
        {
            delayBar.fillAmount = Mathf.MoveTowards(delayBar.fillAmount, targetRatio, drainSpeed * Time.deltaTime);
            yield return null;
        }
        if (delayBar != null) delayBar.fillAmount = targetRatio;
    }

    private void SetImmediate(float ratio)
    {
        if (frontBar != null)
        {
            frontBar.fillAmount = ratio;
            if (changeColorWhenLow) frontBar.color = normalColor;
        }
        if (delayBar != null) delayBar.fillAmount = ratio;
    }
}