using UnityEngine;
using UnityEngine.UI;

public class HPBarSlider : MonoBehaviour
{
    [Header("対象")]
    public Fighter target;

    [Header("スライダー")]
    public Slider hpSlider;
    public Slider damageSlider;

    [Header("演出")]
    public float delay = 0.4f;
    public float speed = 250f;

    private float timer;

    private void Start()
    {
        hpSlider.maxValue = target.maxHP;
        damageSlider.maxValue = target.maxHP;

        hpSlider.value = target.currentHP;
        damageSlider.value = target.currentHP;
    }

    private void Update()
    {
        hpSlider.value = target.currentHP;

        if (damageSlider.value > hpSlider.value)
        {
            timer += Time.deltaTime;

            if (timer >= delay)
            {
                damageSlider.value = Mathf.MoveTowards(
                    damageSlider.value,
                    hpSlider.value,
                    speed * Time.deltaTime);
            }
        }
        else
        {
            damageSlider.value = hpSlider.value;
            timer = 0;
        }
    }
}