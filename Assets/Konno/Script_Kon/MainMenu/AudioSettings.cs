using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    private void Start()
    {
        bgmSlider.value = PlayerPrefs.GetFloat("BGM", 1f);
        seSlider.value = PlayerPrefs.GetFloat("SE", 1f);

        bgmSlider.onValueChanged.AddListener(SetBGM);
        seSlider.onValueChanged.AddListener(SetSE);
    }

    void SetBGM(float value)
    {
        AudioListener.volume = value;

        PlayerPrefs.SetFloat("BGM", value);
    }

    void SetSE(float value)
    {
        PlayerPrefs.SetFloat("SE", value);
    }
}