using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

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

    [SerializeField] private GameObject gameModePanel;  // ゲームモード選択パネル(Player vs Player / Player vs CPU)
    [SerializeField] private GameObject characterRoot;  // キャラクター選択パネルのルートオブジェクト
    [SerializeField] private GameModePanel gameModeManagerPanel;

    [Header("キャラクターPrefab")]
    [SerializeField] private GameObject[] characterPrefabs;
    [Header("キャラクターアイコン画像")]
    [SerializeField] private Sprite[] characterIconSprites;
    [Header("キャラクター表示画像")]
    [SerializeField] private Sprite[] characterPreviewSprites;

    [Header("バトルシーン")]
    [SerializeField] private string battleSceneName = "Character";

    [Header("SE")]
    [SerializeField] private AudioClip moveSe;   // カーソル移動音
    [SerializeField] private AudioClip decideSe; // 決定音
    [SerializeField] private AudioClip cancelSe; // 取消・戻る音

    [Header("バトル確認パネル")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;

    [Header("確認パネル選択肢")]
    [SerializeField] private TMP_Text[] confirmMenuTexts;

    [SerializeField] private Color confirmNormalColor = Color.white;
    [SerializeField] private Color confirmSelectColor = Color.red;

    private int confirmIndex = 0;
    private bool isConfirming = false;
    //[SerializeField] private Sprite[] characterSprites; 画像を使う場合はここに設定する

    private int player1Index = 0;
    private int player2Index = 0;

    private bool player1Decided;
    private bool player2Decided;

    private bool player2Active;

    private Gamepad player1Pad;
    private Gamepad player2Pad;
    private bool cpuMode;
    //private bool selectingCPU;
    private bool canInput = false;
    private bool previousCpuMode;
    private bool isChangingScene = false;

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
    void Start()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }
    private void Update()
    {
        // 確認パネルが開いている間
        // 確認パネル表示中
        if (isConfirming)
        {
            ConfirmInput();
            return;
        }
        // 通常のキャラクター選択
        Player1Input();
        //UpdateGamepads();
        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
             (player1Pad != null &&
                player1Pad.buttonEast.wasPressedThisFrame &&
                selectState == SelectState.Player1 &&
                !player1Decided))
        {
            Debug.Log("CharacterSelectManager : B");
            PlaySe(cancelSe);
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

    private void PlaySe(AudioClip clip)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
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
        {
            player2Active = active;
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
            // 決定済みでも、相手不在(対人戦でP2なし、かつCPUモードでもない)なら
            // ここでBボタンによる取消だけは受け付ける
            if (!cpuMode && !player2Active)
            {
                if (player1Pad.buttonEast.wasPressedThisFrame)
                {
                    player1Decided = false;
                    PlaySe(cancelSe);
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
            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (player1Pad.dpad.right.wasPressedThisFrame)
        {
            player1Index++;
            if (player1Index >= characterIcons.Length)
                player1Index = 0;
            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (player1Pad.buttonSouth.wasPressedThisFrame)
        {
            Debug.Log("Aボタン");

            player1Decided = true;
            PlaySe(decideSe);

            //if (player1Index >= 0 && player1Index < characterPrefabs.Length)
            //{
            //    CharacterSelectionData.Instance.player1Character =
            //        characterPrefabs[player1Index];
            //}
            //else
            //{
            //    Debug.LogError(
            //        "Player1のPrefabが設定されていません。Index = " +
            //        player1Index
            //    );

            //    player1Decided = false;
            //    return;
            //}
            // 仮：Prefab保存なし
            Debug.Log("Player1 キャラクター決定 Index = " + player1Index);

            if (cpuMode)
                selectState = SelectState.CPU;
            else if (player2Active)
                selectState = SelectState.Player2;

            Debug.Log("UpdateSelectionColor前");

            UpdateSelectionColor();

            Debug.Log("UpdateSelectionColor後");
            // 両方決定したか確認
            CheckBothPlayersDecided();
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
                PlaySe(cancelSe);
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
            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (player2Pad.dpad.right.wasPressedThisFrame)
        {
            player2Index++;
            if (player2Index >= characterIcons.Length)
                player2Index = 0;
            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (player2Pad.buttonSouth.wasPressedThisFrame)
        {
            player2Decided = true;
            PlaySe(decideSe);
            //if (player2Index >= 0 && player2Index < characterPrefabs.Length)
            //{
            //    CharacterSelectionData.Instance.player2Character =
            //        characterPrefabs[player2Index];
            //}
            //else
            //{
            //    Debug.LogError(
            //        "Player2のPrefabが設定されていません。Index = " +
            //        player2Index
            //    );

            //    player2Decided = false;
            //    return;
            //}
            // 仮：Prefab保存なし
            // 仮：Prefab保存なし
            Debug.Log("Player2 キャラクター決定 Index = " + player2Index);
            UpdateSelectionColor();
            // 両方決定したか確認
            CheckBothPlayersDecided();
        }

        // 追加: P2未決定中にBを押したらP1選択へ戻る
        if (player2Pad.buttonEast.wasPressedThisFrame)
        {
            player1Decided = false;
            selectState = SelectState.Player1;
            PlaySe(cancelSe);
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

            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (player1Pad.dpad.right.wasPressedThisFrame)
        {
            player2Index++;

            if (player2Index >= characterIcons.Length)
                player2Index = 0;

            PlaySe(moveSe);
            UpdateSelectionColor();
        }

        if (player1Pad.buttonSouth.wasPressedThisFrame)
        {
            player2Decided = true;
            PlaySe(decideSe);
            // CPUが選んだキャラクターを保存
            //if (player2Index >= 0 && player2Index < characterPrefabs.Length)
            //{
            //    CharacterSelectionData.Instance.player2Character =
            //        characterPrefabs[player2Index];
            //}
            //else
            //{
            //    Debug.LogError(
            //        "CPUのPrefabが設定されていません。Index = " +
            //        player2Index
            //    );

            //    player2Decided = false;
            //    return;
            //}
            // 仮：Prefab保存なし
            Debug.Log("CPU キャラクター決定 Index = " + player2Index);
            Debug.Log("CPUキャラクター決定");

            UpdateSelectionColor();
            // 両方決定したか確認
            CheckBothPlayersDecided();
            // TODO : バトルシーンへ
        }
        // Bボタン(取消)
        if (player1Pad.buttonEast.wasPressedThisFrame)
        {
            if (player2Decided)
            {
                // CPU決定取消
                player2Decided = false;
                PlaySe(cancelSe);
                UpdateSelectionColor();

                Debug.Log("CPU決定取消");
            }
            else if (selectState == SelectState.CPU)
            {
                // CPU選択をやめてP1選択へ戻る
                //selectingCPU = false;
                player1Decided = false;
                selectState = SelectState.Player1;
                PlaySe(cancelSe);
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

        CheckBothPlayersDecided();
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

        isChangingScene = false;

        player1Index = 0;
        player2Index = 0;

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
    private void CheckBothPlayersDecided()
    {
        // シーン移動中
        if (isChangingScene)
            return;

        // すでに確認パネル表示中
        if (isConfirming)
            return;

        // Player1とPlayer2の両方が決定した時だけ表示
        if (player1Decided && player2Decided)
        {
            Debug.Log("両方決定 → 確認パネルを開く");

            ShowConfirmPanel();
        }
    }
    private IEnumerator GoToBattleRoutine()
    {
        // フェードアウト
        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.StartFadeOut();
        }

        // Battleシーンへ移動
        SceneManager.LoadScene(battleSceneName);
    }
    private void ConfirmInput()
    {
        if (player1Pad == null)
            return;

        // =========================
        // 左右で選択
        // =========================

        if (player1Pad.dpad.left.wasPressedThisFrame)
        {
            confirmIndex--;

            if (confirmIndex < 0)
                confirmIndex = confirmMenuTexts.Length - 1;

            PlaySe(moveSe);
            UpdateConfirmSelection();

            return;
        }

        if (player1Pad.dpad.right.wasPressedThisFrame)
        {
            confirmIndex++;

            if (confirmIndex >= confirmMenuTexts.Length)
                confirmIndex = 0;

            PlaySe(moveSe);
            UpdateConfirmSelection();

            return;
        }

        // =========================
        // Aボタンで決定
        // =========================

        if (player1Pad.buttonSouth.wasPressedThisFrame)
        {
            PlaySe(decideSe);

            // 0 = スタート
            if (confirmIndex == 0)
            {
                StartBattle();
            }
            // 1 = 戻る
            else if (confirmIndex == 1)
            {
                CloseConfirmPanel();
            }

            return;
        }

        // =========================
        // Bボタンでも戻る
        // =========================

        if (player1Pad.buttonEast.wasPressedThisFrame)
        {
            PlaySe(cancelSe);
            CloseConfirmPanel();
        }
    }
    private void StartBattle()
    {
        if (isChangingScene)
            return;

        Debug.Log("Aボタン → バトル開始");

        isChangingScene = true;
        isConfirming = false;

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }

        FadeManager.Instance.FadeToScene(battleSceneName);
    }
    private void ShowConfirmPanel()
    {
        if (isConfirming)
            return;

        isConfirming = true;

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }

        if (confirmText != null)
        {
            confirmText.text = "これで戦いますか？";
        }

        // 最初は「スタート」を選択
        confirmIndex = 0;

        UpdateConfirmSelection();

        Debug.Log("確認パネル表示");
    }
    private void OpenConfirmPanel()
    {
        Debug.Log("OpenConfirmPanel実行");

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
            Debug.Log("ConfirmPanelを表示しました");
        }
        else
        {
            Debug.LogError("confirmPanelがInspectorで設定されていません");
        }
    }
    private void CloseConfirmPanel()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }

        isConfirming = false;

        // Player2 / CPUを選び直す
        player2Decided = false;

        selectState = cpuMode
            ? SelectState.CPU
            : SelectState.Player2;

        UpdateSelectionColor();

        Debug.Log("確認キャンセル → Player2/CPUを選び直し");
    }
    private void UpdateConfirmSelection()
    {
        if (confirmMenuTexts == null)
            return;

        for (int i = 0; i < confirmMenuTexts.Length; i++)
        {
            if (confirmMenuTexts[i] == null)
                continue;

            if (i == confirmIndex)
            {
                confirmMenuTexts[i].color = confirmSelectColor;
            }
            else
            {
                confirmMenuTexts[i].color = confirmNormalColor;
            }
        }
    }
}