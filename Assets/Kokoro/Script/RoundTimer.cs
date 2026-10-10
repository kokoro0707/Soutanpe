using TMPro;
using UnityEngine;

/// <summary>
/// 1ラウンドの制限時間を管理する。
/// </summary>
public sealed class RoundTimer : MonoBehaviour
{
    [Header("制限時間")]
    [SerializeField, Min(1f)]
    private float roundTime = 99f;


    [Header("UI")]
    [SerializeField]
    private TMP_Text timerText;


    [Header("参照")]
    [SerializeField]
    private FighterHealth player1Health;

    [SerializeField]
    private FighterHealth player2Health;

    [SerializeField]
    private KoSequenceController koSequenceController;




    private float currentTime;

    private bool isRunning;


    private void Start()
    {
        StartTimer();
    }

    private void Awake()
    {
        ResetTimer();
    }


    private void OnEnable()
    {
        if (player1Health != null)
        {
            player1Health.OnKnockedOut += StopTimer;
        }

        if (player2Health != null)
        {
            player2Health.OnKnockedOut += StopTimer;
        }
    }


    private void OnDisable()
    {
        if (player1Health != null)
        {
            player1Health.OnKnockedOut -= StopTimer;
        }

        if (player2Health != null)
        {
            player2Health.OnKnockedOut -= StopTimer;
        }
    }


    private void Update()
    {
        if (!isRunning)
        {
            return;
        }


        currentTime -=
            Time.deltaTime;


        if (currentTime <= 0f)
        {
            currentTime = 0f;

            UpdateText();

            isRunning = false;


            if (koSequenceController != null)
            {
                koSequenceController.HandleTimeUp();
            }

            return;
        }


        UpdateText();
    }


    /// <summary>
    /// FIGHT表示終了時に呼ぶ。
    /// </summary>
    public void StartTimer()
    {
        // タイマーUIが消されていたら再表示
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }

        currentTime = roundTime;
        isRunning = true;

        UpdateText();
    }



    public void StopTimer()
    {
        isRunning = false;
    }


    public void ResetTimer()
    {
        currentTime =
            roundTime;

        isRunning =
            false;

        UpdateText();
    }


    private void UpdateText()
    {
        if (timerText == null)
        {
            return;
        }


        timerText.text =
            Mathf.CeilToInt(
                currentTime
            ).ToString();
    }
}
