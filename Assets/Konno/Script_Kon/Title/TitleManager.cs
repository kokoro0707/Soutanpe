using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("遷移先シーン")]
    [SerializeField] private string nextScene = "MainMenu";

    [Header("キーボード操作")]
    [Tooltip("スタートとして扱うキーボードのキー(複数指定可)")]
    [SerializeField] private Key[] keyboardStartKeys = { Key.Enter, Key.Space };

    [Header("PCデバッグ用キー(任意、キーボード操作とは別に1つだけ追加したい場合)")]
    [SerializeField] private Key debugStartKey = Key.A;

    [Header("演出待ち(任意)")]
    [Tooltip("指定すると、画面展開が終わるまで入力を受け付けない")]
    [SerializeField] private TitleOpenReveal waitForReveal;

    private bool started;
    private string homeSceneName;

    private void Awake()
    {
        homeSceneName = gameObject.scene.name;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        started = false;
        Debug.Log("[TitleManager] 起動しました", this);

        // このオブジェクトがシーンをまたいで残る設定になっていないか確認
        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            Debug.LogWarning(
                "[TitleManager] このオブジェクトは DontDestroyOnLoad(常駐)になっています。" +
                "AudioManager などの常駐スクリプトと同じオブジェクトに付いていないか確認し、" +
                "TitleManager は専用の空オブジェクトに付けてください。", this);
            homeSceneName = SceneManager.GetActiveScene().name;
        }
    }

    // 常駐してしまっている場合の保険:タイトルシーンが読み込まれ直したら受付を再開する
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == homeSceneName)
        {
            started = false;
            Debug.Log("[TitleManager] タイトルが読み込まれたので入力受付を再開", this);
        }
    }

    private void Update()
    {
        // F1キーを押した時だけ表示
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Debug.Log("===== Gamepad Check =====");
            Debug.Log("Gamepad Count : " + Gamepad.all.Count);
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad pad = Gamepad.all[i];
                Debug.Log(
                    $"[{i}] Name:{pad.displayName}  ID:{pad.deviceId}  Interface:{pad.description.interfaceName}"
                );
            }
        }

        if (started)
            return;

        // 常駐している場合、タイトル以外のシーンでは反応しない
        if (SceneManager.GetActiveScene().name != homeSceneName)
            return;

        // フェード中(前のシーンから戻ってきて明るくなっている途中)は受け付けない。
        // ここで押すと FadeToScene が無視され、started だけ true になって操作不能になっていた
        if (FadeManager.Instance != null && FadeManager.Instance.IsFading)
            return;

        if (waitForReveal != null && !waitForReveal.IsFinished)
            return;

        bool gamepadStart =
            Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame;

        bool keyboardStart = IsKeyboardStartPressed();

        bool debugKeyStart =
            Keyboard.current != null &&
            Keyboard.current[debugStartKey].wasPressedThisFrame;

        if (gamepadStart || keyboardStart || debugKeyStart)
        {
            StartGame();
        }
    }

    /// <summary>
    /// keyboardStartKeys に登録されたキーのいずれかが押されたかを判定する。
    /// </summary>
    private bool IsKeyboardStartPressed()
    {
        if (Keyboard.current == null) return false;

        for (int i = 0; i < keyboardStartKeys.Length; i++)
        {
            if (Keyboard.current[keyboardStartKeys[i]].wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private void StartGame()
    {
        started = true;
        Debug.Log("[TitleManager] スタート入力 → " + nextScene, this);

        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeToScene(nextScene);
        else
            SceneManager.LoadScene(nextScene);
    }
}