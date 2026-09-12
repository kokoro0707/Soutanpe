using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SPゲージをストック式で表示する。
/// </summary>
public sealed class FighterSPGaugeUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private FighterSPGauge spGauge;

    [Header("ゲージ画像")]
    [SerializeField]
    private Image[] gaugeImages;


    private void Start()
    {
        if (spGauge == null)
        {
            return;
        }

        spGauge.OnSPChanged +=
            UpdateGauge;

        UpdateGauge(
            spGauge.CurrentSP,
            spGauge.MaxSP
        );
    }


    private void OnDestroy()
    {
        if (spGauge != null)
        {
            spGauge.OnSPChanged -=
                UpdateGauge;
        }
    }


    private void UpdateGauge(
        int current,
        int max
    )
    {
        for (int i = 0;
             i < gaugeImages.Length;
             i++)
        {
            if (gaugeImages[i] == null)
            {
                continue;
            }

            // 現在SPより小さい番号だけ表示
            gaugeImages[i].enabled =
                i < current;
        }
    }
}
