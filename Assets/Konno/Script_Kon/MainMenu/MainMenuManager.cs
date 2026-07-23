using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private TMP_Text[] menuTexts;

    [SerializeField] private string nextScene = "CharacterSelection";

    private int currentIndex = 0;

    private bool inputLock;

    private void Start()
    {
        UpdateSelection();
    }

    private void Update()
    {
        Move();

        if (Keyboard.current.aKey.wasPressedThisFrame ||
            (Gamepad.current != null &&
             Gamepad.current.buttonSouth.wasPressedThisFrame))
        {
            Execute();
        }
    }

    void Move()
    {
        if (inputLock) return;

        bool left =
            Keyboard.current.leftArrowKey.wasPressedThisFrame ||
            (Gamepad.current != null &&
             Gamepad.current.dpad.left.wasPressedThisFrame);

        bool right =
            Keyboard.current.rightArrowKey.wasPressedThisFrame ||
            (Gamepad.current != null &&
             Gamepad.current.dpad.right.wasPressedThisFrame);

        if (left)
        {
            currentIndex--;

            if (currentIndex < 0)
                currentIndex = menuTexts.Length - 1;

            UpdateSelection();
        }

        if (right)
        {
            currentIndex++;

            if (currentIndex >= menuTexts.Length)
                currentIndex = 0;

            UpdateSelection();
        }
    }

    void UpdateSelection()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            if (i == currentIndex)
            {
                menuTexts[i].color = Color.red;
                menuTexts[i].fontSize = 48;
            }
            else
            {
                menuTexts[i].color = Color.white;
                menuTexts[i].fontSize = 40;
            }
        }
    }

    void Execute()
    {
        switch (currentIndex)
        {
            case 0:

                SceneManager.LoadScene(nextScene);

                break;

            case 1:

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif

                break;
        }
    }
}