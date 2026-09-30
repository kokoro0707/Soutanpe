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
            Debug.LogError(
                $"{name}：SPGaugeが設定されていません。",
                this
            );

            return;
        }

        // SP変更イベントを受け取る
        spGauge.OnSPChanged += UpdateGauge;

        // 最初の表示
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
        int current,
        int max
    )
    {
        Debug.Log(
            $"{name}：SP UI更新 " +
            $"Current={current} / Max={max}",
            this
        );

        for (int i = 0;
             i < gaugeImages.Length;
             i++)
        {
            if (gaugeImages[i] == null)
            {
                Debug.LogWarning(
                    $"{name}：GaugeImages[{i}]がNULL",
                    this
                );

                continue;
            }

            // SPに応じてFill自体を表示・非表示
            gaugeImages[i].gameObject.SetActive(
                i < current
            );
        }
    }
}
