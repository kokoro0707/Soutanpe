using System;

[Serializable]
public struct FighterInputData
{
    public int horizontal;
    public int vertical;

    public bool jumpPressed;

    // 通常コンボ
    public bool lightAttackPressed;
    public bool heavyAttackPressed;

    // アシストコンボ専用
    public bool assistComboPressed;

    //必殺ボタン
    public bool specialAttackPressed;
}
