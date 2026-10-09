using UnityEngine;
using UnityEngine.UI;

public sealed class FighterSPGaugeUI : MonoBehaviour
{
    [SerializeField]
    private FighterSPGauge spGauge;

    [SerializeField]
    private Image fillImage;

    private void Start()
    {
        if (spGauge == null)
        {
            Debug.LogError(
                $"{name}: SPGauge‚ªİ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ",
                this
            );

            return;
        }

        if (fillImage == null)
        {
            Debug.LogError(
                $"{name}: Fill Image‚ªİ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ",
                this
            );

            return;
        }

        spGauge.OnSPChanged += UpdateGauge;

        UpdateGauge(
            spGauge.CurrentSP,
            spGauge.MaxSP
        );
    }

    private void OnDestroy()
    {
        if (spGauge != null)
        {
            spGauge.OnSPChanged -= UpdateGauge;
        }
    }

    private void UpdateGauge(
        float current,
        float max
    )
    {
        if (fillImage == null)
        {
            return;
        }

        // 0 ` 1 ‚É•ÏŠ·
        fillImage.fillAmount =
            current / max;

    }
}
