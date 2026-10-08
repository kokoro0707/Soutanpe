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
                $"{name}: SPGaugeÇ™ê›íËÇ≥ÇÍÇƒÇ¢Ç‹ÇπÇÒ",
                this
            );

            return;
        }

        if (fillImage == null)
        {
            Debug.LogError(
                $"{name}: Fill ImageÇ™ê›íËÇ≥ÇÍÇƒÇ¢Ç‹ÇπÇÒ",
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

        // 0 Å` 1 Ç…ïœä∑
        fillImage.fillAmount =
            current / max;

        Debug.Log(
            $"SP UI : {current:F2} / {max:F2} " +
            $"Fill={fillImage.fillAmount:F2}",
            this
        );
    }
}
