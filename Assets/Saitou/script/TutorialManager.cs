using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialManager02 : MonoBehaviour
{
    public enum TutorialStep
    {
        start,
        movement,
        jump,
        lightattack,
        strongattack,
        guard,
        special,
        battle,
        result
    }

    private TutorialStep tutorialstep = TutorialStep.start;

    [SerializeField]
    private TutorialP2InputSource player02TutorialInput;
    [SerializeField]
    private CPUFighterInputSource player02CPU;
    [SerializeField]
    private FighterController player01;
    [SerializeField]
    private FighterController player02;
    [SerializeField]
    private float forwardRequiredTime = 3.0f;

    [SerializeField]
    private float backwardRequiredTime = 3.0f;
    [SerializeField]
    private FighterStateMachine stateMachine;
    [SerializeField]
    private int RequiredJumpCount = 3;
    [SerializeField]
    private int RequiredlightAttackCount = 3;
    [SerializeField]
    private int RequiredStrongAttackCount = 3;
    [SerializeField]
    private int RequiredGuardCount = 3;

    [SerializeField]
    private FighterMoveSet player01MoveSet;

    [SerializeField]
    private int RequiredForwardSpecialCount = 3;

    [SerializeField]
    private int RequiredDownSpecialCount = 3;

    private int forwardSpecialCount = 0;
    private int downSpecialCount = 0;

    private int player02HPBeforeSpecial = 0;

    private bool specialStarted = false;
    private bool specialHit = false;

    private MoveData currentSpecialMove = null;
    private bool waitingForSpecialEnd = false;

    private int guardCount = 0;
    private bool wasBlockStun = false;
    private bool wasJumping = false;
    private int jumpCount = 0;
    private int lightAttackCount = 0;
    private FighterMoveController player01MoveController;
    private FighterHealth player02Health;
    private FighterHealth player01Health;
    private FighterStateMachine player02StateMachine;

    private bool lightSecondStarted = false;
    private int player02HPBeforeSecondLight = 0;
    private int strongAttackCount = 0;
    private bool strongThirdActive = false;
    private int player02HPBeforeThirdStrong = 0;


    public GameObject panel;
    public GameObject minipanel;
    public TMP_Text text;
    public TMP_Text minitext;
    public GameObject upimage;
    public GameObject downimage;
    public GameObject rightimage;
    public GameObject leftimage;
    public GameObject xbuttonimage;
    public GameObject xbuttonimage2;
    public GameObject xbuttonimage3;
    public GameObject ybuttonimage;
    public GameObject ybuttonimage2;
    public GameObject ybuttonimage3;
    public GameObject abuttonimage;
    public GameObject abuttonimage2;
    private float forwardTime = 0.0f;
    private float backwardTime = 0.0f;
    private bool forwardCompleted = false;
    private bool backwardCompleted = false;

    public void HidePanel()
    {
        panel.SetActive(false);
    }
    public void HideMiniPanel()
    {
        minipanel.SetActive(false);
    }

    public void InPanel()
    {
        panel.SetActive(true);
    }
    public void InMiniPanel()
    {
        minipanel.SetActive(true);
    }

    private void Start()
    {



        if (player01 != null)
        {
            player01.SetUseLocalInput(false);
        }
        if (player02 != null)
        {
            player02.SetUseLocalInput(false);
        }



        player02.gameObject.SetActive(false);
        HideMiniPanel();
        stateMachine = player01.GetComponent<FighterStateMachine>();
        upimage.SetActive(false);
        xbuttonimage.SetActive(false);
        xbuttonimage2.SetActive(false);
        xbuttonimage3.SetActive(false);

        player01MoveController = player01.GetComponent<FighterMoveController>();
        player02Health = player02.GetComponent<FighterHealth>();
        player02StateMachine = player02.GetComponent<FighterStateMachine>();


        text.text = "チュートリアル開始";
        player01Health = player01.GetComponent<FighterHealth>();
        player02Health = player02.GetComponent<FighterHealth>();

    }

    private void Update()
    { 


        switch (tutorialstep)
        {

            case TutorialStep.start:


                bool controllerInput = Gamepad.current != null
                    && Gamepad.current.buttonSouth.wasPressedThisFrame;

                bool keyboardInput = Keyboard.current != null
                    && Keyboard.current.aKey.wasPressedThisFrame;

                if (controllerInput || keyboardInput)
                {
                    HidePanel();
                    InMiniPanel();
                    minitext.text = "  前後に移動";
                    player01.SetUseLocalInput(true);
                    tutorialstep = TutorialStep.movement;
                }

                break;

            case TutorialStep.movement:

                float stickX = 0.0f;
                bool dpadLeft = false;
                bool dpadRight = false;
                if (Gamepad.current != null)
                {
                    stickX = Gamepad.current.leftStick.x.ReadValue();
                    dpadLeft = Gamepad.current.dpad.left.isPressed;
                    dpadRight = Gamepad.current.dpad.right.isPressed;
                }

                bool keyboardLeft =
                    Keyboard.current != null &&
                    Keyboard.current.aKey.isPressed;

                bool keyboardRight =
                    Keyboard.current != null &&
                    Keyboard.current.dKey.isPressed;


                bool leftInput = false;
                if ((stickX < -0.5f || dpadLeft) || keyboardLeft)
                {
                    leftInput = true;
                }

                bool rightInput =
                    stickX > 0.5f ||
                    dpadRight;
                if ((stickX > 0.5f || dpadRight) || keyboardRight)
                {
                    rightInput = true;
                }

                if (leftInput && !rightInput)
                {
                    Debug.Log("左入力");
                    backwardTime += Time.deltaTime;
                    if (backwardTime >= backwardRequiredTime)
                    {
                        backwardCompleted = true;
                        Debug.Log("左OK");
                    }
                }

                if (rightInput && !leftInput)
                {
                    Debug.Log("右入力");
                    forwardTime += Time.deltaTime;
                    if (forwardTime >= forwardRequiredTime)
                    {
                        forwardCompleted = true;
                        Debug.Log("右OK");
                    }
                }

                if (backwardCompleted && forwardCompleted)
                {
                    tutorialstep = TutorialStep.jump;
                    minitext.text = "  ジャンプ";
                    rightimage.SetActive(false);
                    leftimage.SetActive(false);
                    upimage.SetActive(true);

                }

                break;

            case TutorialStep.jump:

                bool isJumping = stateMachine.CurrentState == FighterState.Jump;

                if (isJumping && !wasJumping)
                {
                    jumpCount++;

                    Debug.Log("ジャンプを検知しました！ 回数: " + jumpCount);
                    if (jumpCount >= RequiredJumpCount)
                    {
                        Debug.Log("ジャンプOK！");
                        lightSecondStarted = false;
                        player02HPBeforeSecondLight = 0;

                        minitext.text = "  弱攻撃\n  2段まで";
                        upimage.SetActive(false);
                        xbuttonimage.SetActive(true);
                        xbuttonimage2.SetActive(true);
                        xbuttonimage3.SetActive(true);
                        tutorialstep = TutorialStep.lightattack;
                        player02.gameObject.SetActive(true);

                    }
                }

                wasJumping = isJumping;

                break;

            case TutorialStep.lightattack:


                if (player01MoveController == null || player02Health == null)
                {
                    break;
                }

                bool secondLightAttack =
                    player01MoveController.CurrentComboType == FighterComboType.Light
                    && player01MoveController.CurrentComboIndex >= 1;

                if (secondLightAttack && !lightSecondStarted)
                {
                    lightSecondStarted = true;

                    player02HPBeforeSecondLight = player02Health.CurrentHP;



                    Debug.Log(
                        "弱攻撃2段目開始！ 開始時HP: "
                        + player02HPBeforeSecondLight
                    );
                }

                if (lightSecondStarted)
                {
                    if (player02Health.CurrentHP < player02HPBeforeSecondLight)
                    {
                        Debug.Log("弱攻撃2段目ヒット！ OK！");

                        lightAttackCount += 1;
                        player02Health.ResetHealth();
                        lightSecondStarted = false;
                    }
                }

                if (lightAttackCount >= RequiredlightAttackCount)
                {
                    Debug.Log("弱攻撃チュートリアルOK！");

                    Debug.Log("saiso0");
                    minitext.text = "  強攻撃\n  3段まで";

                    strongThirdActive = false;
                    player02HPBeforeThirdStrong = 0;

                    ybuttonimage.SetActive(true);
                    ybuttonimage2.SetActive(true);
                    ybuttonimage3.SetActive(true);
                    xbuttonimage.SetActive(false);
                    xbuttonimage2.SetActive(false);
                    xbuttonimage3.SetActive(false);



                    Debug.Log("X1: " + xbuttonimage.name + " / " + xbuttonimage.activeSelf);
                    Debug.Log("X2: " + xbuttonimage2.name + " / " + xbuttonimage2.activeSelf);
                    Debug.Log("X3: " + xbuttonimage3.name + " / " + xbuttonimage3.activeSelf);

                    Debug.Log("saiso");

                    tutorialstep = TutorialStep.strongattack;

                    Debug.Log("saiso2");
                }

                break;

            case TutorialStep.strongattack:

                if (player01MoveController == null || player02Health == null)
                {
                    break;
                }

                Debug.Log("saiso3");

                Debug.Log(
        "強攻撃確認：" +
        " ComboType=" + player01MoveController.CurrentComboType +
        " ComboIndex=" + player01MoveController.CurrentComboIndex +
        " CurrentMove=" + player01MoveController.CurrentMove
    );

                // 現在、本当に強攻撃3段目の攻撃判定が出ているか
                bool isThirdStrongActive =
                    player01MoveController.CurrentComboType == FighterComboType.Heavy
                    && player01MoveController.CurrentComboIndex == 2
                    && player01MoveController.CurrentMove != null
                    && player01MoveController.CurrentMove.IsActiveFrame(
                        player01MoveController.CurrentMoveFrame
                    );

                // 3段目の攻撃判定が出始めた瞬間
                if (isThirdStrongActive && !strongThirdActive)
                {
                    strongThirdActive = true;

                    player02HPBeforeThirdStrong =
                        player02Health.CurrentHP;

                    Debug.Log(
                        "強攻撃3段目の攻撃判定開始！ 開始時HP: "
                        + player02HPBeforeThirdStrong
                    );
                }

                // 3段目の攻撃判定が出ている間だけHP減少を確認
                if (isThirdStrongActive && strongThirdActive)
                {
                    if (player02Health.CurrentHP < player02HPBeforeThirdStrong)
                    {
                        Debug.Log("強攻撃3段目ヒット！ OK！");

                        strongAttackCount++;

                        player02Health.ResetHealth();

                        Debug.Log(
                            "強攻撃クリア回数: "
                            + strongAttackCount
                            + " / "
                            + RequiredStrongAttackCount
                        );

                        strongThirdActive = false;
                    }
                }

                // 3段目の攻撃判定が終わったら、
                // ヒットしていなくても今回の判定を終了
                if (!isThirdStrongActive)
                {
                    strongThirdActive = false;
                }

                if (strongAttackCount >= RequiredStrongAttackCount)
                {
                    Debug.Log("強攻撃チュートリアルOK！");

                    ybuttonimage.SetActive(false);
                    ybuttonimage2.SetActive(false);
                    ybuttonimage3.SetActive(false);

                    minitext.text = "  ガード  後ろ入力";

                    tutorialstep = TutorialStep.guard;

                    if (player02TutorialInput != null)
                    {
                        player02.SetInputSource(player02TutorialInput);
                    }
                }

                break;

            case TutorialStep.guard:

                player01Health.ResetHealth();

                bool isBlockStun =
                    stateMachine.CurrentState == FighterState.BlockStun;

                // 実際に攻撃をガードして
                // BlockStunになった瞬間だけカウント
                if (isBlockStun && !wasBlockStun)
                {
                    guardCount++;

                    Debug.Log(
                        $"P2の攻撃をガード成功！ " +
                        $"{guardCount} / {RequiredGuardCount}"
                    );

                    minitext.text =
                        $"  ガード {guardCount} / {RequiredGuardCount}";
                }

                wasBlockStun = isBlockStun;

                if (guardCount >= RequiredGuardCount)
                {
                    Debug.Log("ガードチュートリアルOK！");

                    player02.SetUseLocalInput(false);

                    minitext.text = "  横必殺技  前入力  +  \n  下必殺技";

                    tutorialstep = TutorialStep.special;
                    abuttonimage.SetActive(true);
                    abuttonimage2.SetActive(true);
                    downimage.SetActive(true);
                }

                break;

            case TutorialStep.special:

                if (player01MoveController == null ||
                    player02Health == null ||
                    player01MoveSet == null)
                {
                    break;
                }

                // 必殺技が開始した瞬間
                if (player01MoveController.CurrentMove != null &&
                    !specialStarted &&
                    !waitingForSpecialEnd)
                {
                    MoveData currentMove =
                        player01MoveController.CurrentMove;

                    // 横必殺技
                    if (currentMove == player01MoveSet.ForwardSpecial)
                    {
                        specialStarted = true;
                        specialHit = false;
                        currentSpecialMove = currentMove;

                        player02HPBeforeSpecial =
                            player02Health.CurrentHP;

                        Debug.Log("横必殺技開始！");
                    }

                    // 下必殺技
                    else if (currentMove == player01MoveSet.DownSpecial)
                    {
                        specialStarted = true;
                        specialHit = false;
                        currentSpecialMove = currentMove;

                        player02HPBeforeSpecial =
                            player02Health.CurrentHP;

                        Debug.Log("下必殺技開始！");
                    }
                }

                // 必殺技が実際にヒットしたか確認
                if (specialStarted && !specialHit)
                {
                    if (player02Health.CurrentHP <
                        player02HPBeforeSpecial)
                    {
                        specialHit = true;

                        if (currentSpecialMove ==
                            player01MoveSet.ForwardSpecial)
                        {
                            forwardSpecialCount++;

                            Debug.Log(
                                "横必殺技ヒット！ "
                                + forwardSpecialCount
                                + " / "
                                + RequiredForwardSpecialCount
                            );
                        }
                        else if (currentSpecialMove ==
                                 player01MoveSet.DownSpecial)
                        {
                            downSpecialCount++;

                            Debug.Log(
                                "下必殺技ヒット！ "
                                + downSpecialCount
                                + " / "
                                + RequiredDownSpecialCount
                            );
                        }

                        // 次の攻撃を判定できるようにHPを戻す
                        player02Health.ResetHealth();

                        specialStarted = false;
                        specialHit = false;
                        currentSpecialMove = null;

                        waitingForSpecialEnd = true;
                    }
                }

                if (waitingForSpecialEnd &&
                    player01MoveController.CurrentMove == null)
                {
                    waitingForSpecialEnd = false;
                }

                // 両方の必殺技を必要回数当てたらクリア
                if (forwardSpecialCount >=
                        RequiredForwardSpecialCount &&
                    downSpecialCount >=
                        RequiredDownSpecialCount)
                {
                    Debug.Log("必殺技チュートリアルOK！");

                    minitext.text = "  必殺技OK！";
                    HideMiniPanel();


                    tutorialstep = TutorialStep.battle;

                    player01Health.ResetHealth();
                    player02Health.ResetHealth();

                }

                break;

            case TutorialStep.battle:

                // P2をCPU操作に切り替える
                if (player02CPU != null)
                {
                    // CPUに「P1が相手」と教える
                    player02CPU.SetOpponent(player01.transform);

                    // P2の入力をCPUに変更
                    player02.SetInputSource(player02CPU);
                }

                // どちらかのHPが0になったら結果画面へ
                if (player01Health != null && player02Health != null)
                {
                    if (player01Health.CurrentHP <= 0 || player02Health.CurrentHP <= 0)
                    {
                        tutorialstep = TutorialStep.result;
                        text.text = "再戦";
                        Debug.Log("どちらかのHPが0になりました。RESULTへ移行します。");
                        HideMiniPanel();
                        InPanel();
                    }
                }


                break;

            case TutorialStep.result:

                // コントローラーのAボタン
                bool controllerRetry =
                    Gamepad.current != null &&
                    Gamepad.current.buttonSouth.wasPressedThisFrame;

                // キーボードのAキー
                bool keyboardRetry =
                    Keyboard.current != null &&
                    Keyboard.current.aKey.wasPressedThisFrame;

                // Aボタン / Aキーが押されたら再戦
                if (controllerRetry || keyboardRetry)
                {

                    Time.timeScale = 1f;

                    Debug.Log("A入力を検知しました。再戦します！");

                    // P1のHPを全回復
                    if (player01Health != null)
                    {
                        player01Health.ResetHealth();
                    }

                    // P2のHPを全回復
                    if (player02Health != null)
                    {
                        player02Health.ResetHealth();
                    }

                    // P2を表示
                    if (player02 != null)
                    {
                        player02.gameObject.SetActive(true);
                    }

                    // P2をCPU操作に戻す
                    if (player02CPU != null)
                    {
                        player02CPU.SetOpponent(player01.transform);
                        player02.SetInputSource(player02CPU);
                    }

                    // バトルへ戻る
                    tutorialstep = TutorialStep.battle;

                    // RESULTパネルを消す
                    HidePanel();

                    Debug.Log("再戦開始！");
                }


                break;



        }


    }





}