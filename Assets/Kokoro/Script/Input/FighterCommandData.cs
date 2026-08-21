public struct FighterCommandData
{
    public int horizontal;
    public int vertical;

    public bool jumpPressed;

    // 通常コンボ
    public bool lightAttackPressed;
    public bool heavyAttackPressed;

    // アシストコンボ
    public bool assistComboPressed;

    //必殺技
    public bool forwardSpecialPressed;
    public bool downSpecialPressed;

    //つかみ
    public bool grabPressed;

    // 移動系コマンド
    public bool guardHeld;
    public bool forwardStepPressed;
    public bool backStepPressed;
    public bool dashHeld;
}
