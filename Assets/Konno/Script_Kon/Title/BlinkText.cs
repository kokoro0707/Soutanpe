using TMPro;
using UnityEngine;

public class BlinkText : MonoBehaviour
{
    [SerializeField] private float blinkSpeed = 2f;
    [SerializeField] private float minAlpha = 0.4f;
    [SerializeField] private float maxAlpha = 1f;

    private TextMeshProUGUI textUI;

    private void Awake()
    {
        textUI = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        Color color = textUI.color;

        float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
        color.a = Mathf.Lerp(minAlpha, maxAlpha, t);

        textUI.color = color;
    }
}