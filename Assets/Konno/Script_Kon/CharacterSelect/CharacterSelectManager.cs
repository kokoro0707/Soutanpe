using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("キャラクターアイコン")]
    [SerializeField] private Image[] characterIcons; // キャラクターアイコンのImageコンポーネントを配列で設定する

    [Header("カラー設定")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color player1Color = Color.red;
    [SerializeField] private Color player2Color = Color.blue;
    [SerializeField] private Color decidedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
   
    [Header("ラベル")]
    [SerializeField] private TMP_Text player1Label;
    [SerializeField] private TMP_Text player2Label;

    [Header("立ち絵")]
    [SerializeField] private Image player1Preview;
    [SerializeField] private Image player2Preview;
        
    [SerializeField] private GameObject gameModePanel;  // ゲームモード選択パネル（Player vs Player / Player vs CPU）
    [SerializeField] private GameObject characterRoot;  // キャラクター選択パネルのルートオブジェクト
    [SerializeField] private GameModePanel gameModeManagerPanel;

    [Header("キャラクターPrefab")]
    [SerializeField] private GameObject[] characterPrefabs;
    [Header("キャラクターアイコン画像")]
    [SerializeField] private Sprite[] characterIconSprites;
    [Header("キャラクター表示画像")]
    [SerializeField] private Sprite[] characterPreviewSprites;

    //[SerializeField] private Sprite[] characterSprites; 画像を使う場合はここに設定する

    private int player1Index = 0;
    private int player2Index = 1;

    private bool player1Decided;
    private bool player2Decided;

    private bool player2Active;

    private Gamepad player1Pad;
    private Gamepad player2Pad;
    private bool cpuMode;
    //private bool selectingCPU;
    private bool canInput = false;
    private bool previousCpuMode;


    [SerializeField]
    private Color[] characterColors =
{
    Color.red,
    Color.blue,
    Color.yellow,
    Color.green
};
    private enum SelectState
    {
        Player1,
        Player2,
        CPU
    }

    private SelectState selectState = SelectState.Player1;
    private void OnEnable()
    {
        //UpdateGamepads();
        //UpdateSelectionColor();

        Initialize();
        //StartCoroutine(WaitReleaseButton());
    }
    private void Update()
    {
        //UpdateGamepads();
        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
             (player1Pad != null &&
                player1Pad.buttonEast.wasPressedThisFrame &&
                selectState == SelectState.Player1 &&
                !player1Decided))
        {
            Debug.Log("CharacterSelectManager : B");
            BackToGameMode();
            return;
        }
        cpuMode = GameModeManager.Instance.CurrentMode ==
             GameModeManager.Mode.PlayerVsCPU;

        if (cpuMode != previousCpuMode)
        {
            //selectingCPU = false;
            player2Decided = false;
            previousCpuMode = cpuMode;

            UpdateSelectionColor();
        }

        if (!canInput)
            return;

        UpdateGamepads();

        //Debug.Log(player1Pad);
        switch (selectState)
        {
            case SelectState.Player1:
                if (player1Pad != null)
                    Player1Input();
                break;

            case SelectState.Player2:
                if (player2Pad != null)
                    Player2Input();
                break;

            case SelectState.CPU:
                if (player1Pad != null)
                    CPUInput();
                break;
        }
    }
    private void BackToGameMode()
    {
        StartCoroutine(BackToGameModeRoutine());
    }

    private IEnumerator BackToGameModeRoutine()
    {
        // フェードアウト
        yield return FadeManager.Instance.StartFadeOut(0.2f);

        // パネル切り替え
        characterRoot.SetActive(false);
        gameModePanel.SetActive(true);

        // 初期化
        //gameModeManagerPanel.Initialize();

        // フェードイン
        FadeManager.Instance.StartFadeIn(0.2f);
    }
    private void UpdateGamepads()
    {
        player1Pad = null;
        player2Pad = null;

        // P1は常に1台目
        if (Gamepad.all.Count >= 1)
            player1Pad = Gamepad.all[0];
        if (Gamepad.all.Count >= 2)
            player2Pad = Gamepad.all[1];

        bool active = player2Pad != null; 
        if (active != player2Active) 
        { player2Active = active; 
            if (!player2Active) 
            { 
                player2Decided = false; 
            } 
            UpdateSelectionColor(); 
        }
    }

    private void Player1Input()
    {
        if (player1Decided)
        {
            // 決定済みでも、相手不在（対人戦でP2なし、かつCPUモードでもない）なら
            // ここでBボタンによる取消だけは受け付ける
            if (!cpuMode && !player2Active)
            {
                if (player1Pad.buttonEast.wasPressedThisFrame)
                {
                    player1Decided = false;
                    UpdateSelectionColor();

                    Debug.Log("P1 決定取消");
                }
            }
            return;
        }

        if (player1Pad.dpad.left.wasPressedThisFrame)
        {
            player1Index--;
            if (player1Index < 0)
                player1Index = characterIcons.Length - 1;
            UpdateSelectionColor();
        }

        if (player1Pad.dpad.right.wasPressedThisFrame)
        {
            player1Index++;
            if (player1Index >= characterIcons.Length)
                player1Index = 0;
            UpdateSelectionColor();
        }

        if (player1Pad.buttonSouth.wasPressedThisFrame)
        {
            Debug.Log("Aボタン");

            player1Decided = true;

            // Player1が選んだキャラクターを保存
            CharacterSelectionData.Instance.player1Character =
    characterPrefabs[player1Index];

            if (cpuMode)
                selectState = SelectState.CPU;
            else if (player2Active)
                selectState = SelectState.Player2;

            Debug.Log("UpdateSelectionColor前");

            UpdateSelectionColor();

            Debug.Log("UpdateSelectionColor後");
        }
    }

    private void Player2Input()
    {
        if (player2Decided)
        {
            // 決定済みなら取消のみ受け付ける
            if (player2Pad.buttonEast.wasPressedThisFrame)
            {
                player2Decided = false;
                selectState = SelectState.Player2;
                UpdateSelectionColor();

                Debug.Log("P2 決定取消");
            }
            return;
        }

        if (player2Pad.dpad.left.wasPressedThisFrame)
        {
            player2Index--;
            if (player2Index < 0)
                player2Index = characterIcons.Length - 1;
            UpdateSelectionColor();
        }

        if (player2Pad.dpad.right.wasPressedThisFrame)
        {
            player2Index++;
            if (player2Index >= characterIcons.Length)
                player2Index = 0;
            UpdateSelectionColor();
        }

        if (player2Pad.buttonSouth.wasPressedThisFrame)
        {
            player2Decided = true;
            // Player2が選んだキャラクターを保存
            CharacterSelectionData.Instance.player2Character =
        characterPrefabs[player2Index];
            UpdateSelectionColor();
        }

        // 追加: P2未決定中にBを押したらP1選択へ戻る
        if (player2Pad.buttonEast.wasPressedThisFrame)
        {
            player1Decided = false;
            selectState = SelectState.Player1;
            UpdateSelectionColor();

            Debug.Log("P1選択へ戻る");
        }
    }
    private void CPUInput()
    {
        // まだ1Pが決定していないなら何もしない
        if (!player1Decided)
            return;

        // 1P決定後にCPU選択開始
        //selectingCPU = true;

        if (player1Pad.dpad.left.wasPressedThisFrame)
        {
            player2Index--;

            if (player2Index < 0)
                player2Index = characterIcons.Length - 1;

            UpdateSelectionColor();
        }

        if (player1Pad.dpad.right.wasPressedThisFrame)
        {
            player2Index++;

            if (player2Index >= characterIcons.Length)
                player2Index = 0;

            UpdateSelectionColor();
        }

        if (player1Pad.buttonSouth.wasPressedThisFrame)
        {
            player2Decided = true;
            // CPUが選んだキャラクターを保存
            CharacterSelectionData.Instance.player2Character =
                characterPrefabs[player2Index];

            Debug.Log("CPUキャラクター決定");

            UpdateSelectionColor();

            // TODO : バトルシーンへ
        }
        // Bボタン（取消）
        if (player1Pad.buttonEast.wasPressedThisFrame)
        {
            if (player2Decided)
            {
                // CPU決定取消
                player2Decided = false;
                UpdateSelectionColor();

                Debug.Log("CPU決定取消");
            }
            else if (selectState == SelectState.CPU)
            {
                // CPU選択をやめてP1選択へ戻る
                //selectingCPU = false;
                player1Decided = false;
                selectState = SelectState.Player1;
                UpdateSelectionColor();

                Debug.Log("P1選択へ戻る");
            }
        }
    }
    private void UpdateSelectionColor()
    {
        // 全員白
        foreach (Image icon in characterIcons)
        {
            icon.color = normalColor;
        }

        // ===== PLAYER1 =====
        player1Label.gameObject.SetActive(true);
        player1Label.text = "PLAYER";
        player1Label.color = player1Color;

        // 選択中のキャラの上へ移動
        player1Label.rectTransform.position =
            characterIcons[player1Index].rectTransform.position + new Vector3(0, 80, 0);

        if (player1Decided)
            characterIcons[player1Index].color = decidedColor;
        else
            characterIcons[player1Index].color = player1Color;

        // ===== PLAYER2 / CPU =====
        if (cpuMode)
        {
            if (selectState == SelectState.CPU)
            {
                player2Label.gameObject.SetActive(true);
                player2Label.text = "CPU";
                player2Label.color = Color.yellow;

                player2Label.rectTransform.position =
                    characterIcons[player2Index].rectTransform.position + new Vector3(0, 80, 0);

                if (player2Decided)
                    characterIcons[player2Index].color = decidedColor;
                else
                    characterIcons[player2Index].color = Color.yellow;
            }
            else
            {
                player2Label.gameObject.SetActive(false);
            }
        }
        else if (player2Active)
        {
            player2Label.gameObject.SetActive(true);
            player2Label.text = "PLAYER";
            player2Label.color = player2Color;

            player2Label.rectTransform.position =
                characterIcons[player2Index].rectTransform.position + new Vector3(0, 80, 0);

            if (player2Decided)
                characterIcons[player2Index].color = decidedColor;
            else
                characterIcons[player2Index].color = player2Color;
        }
        else
        {
            player2Label.gameObject.SetActive(false);
        }
        //UpdateSelectionColor();
        UpdatePreview();
    }
    private IEnumerator WaitReleaseButton()
    {
        canInput = false;

        while (true)
        {
            UpdateGamepads();

            // Padが無い場合
            if (player1Pad == null)
            {
                canInput = true;
                yield break;
            }

            // Aボタンを離したら入力開始
            if (!player1Pad.buttonSouth.isPressed)
            {
                break;
            }

            yield return null;
        }

        canInput = true;
    }
    public void Initialize()
    {
        cpuMode =
            GameModeManager.Instance.CurrentMode ==
            GameModeManager.Mode.PlayerVsCPU;

        selectState = SelectState.Player1;

        player1Decided = false;
        player2Decided = false;

        player1Index = 0;
        player2Index = 1;

        previousCpuMode = cpuMode;
        canInput = true;
        SetupCharacterIcons();
        UpdateGamepads();
        UpdateSelectionColor();
    }
    private void SetupCharacterIcons()
    {
        int count = Mathf.Min(characterIcons.Length, characterIconSprites.Length);

        for (int i = 0; i < count; i++)
        {
            characterIcons[i].sprite = characterIconSprites[i];
        }
    }
    private void UpdatePreview()
    {
        // ===== PLAYER1 =====
        if (player1Decided &&
            player1Index >= 0 &&
            player1Index < characterPreviewSprites.Length)
        {
            player1Preview.gameObject.SetActive(true);

            // 選択キャラクターの画像を設定
            player1Preview.sprite =
                characterPreviewSprites[player1Index];

            // 元の色で表示
            player1Preview.color = Color.white;
        }
        else
        {
            player1Preview.gameObject.SetActive(false);
        }


        // ===== PLAYER2 / CPU =====
        if (player2Decided &&
            player2Index >= 0 &&
            player2Index < characterPreviewSprites.Length)
        {
            player2Preview.gameObject.SetActive(true);

            // 選択キャラクターの画像を設定
            player2Preview.sprite =
                characterPreviewSprites[player2Index];

            // 元の色で表示
            player2Preview.color = Color.white;
        }
        else
        {
            player2Preview.gameObject.SetActive(false);
        }
    }
}