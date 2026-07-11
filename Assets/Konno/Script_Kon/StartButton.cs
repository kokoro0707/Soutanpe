using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    [Header("遷移先のシーン名")]
    [SerializeField] private string sceneName = "Main";

    // ボタンのOnClickイベントに登録する関数
    public void OnStartButtonClicked()
    {
        Debug.Log("スタートボタンが押されました");
        SceneManager.LoadScene("Main");
    }
}