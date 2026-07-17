using System;

/// <summary>
/// 1フレーム分のキャラクター入力情報。
/// ローカル入力でもオンライン入力でも同じ形式を使用する。
/// </summary>
[Serializable]
public struct FighterInputData
{
    /// <summary>
    /// -1：左、0：入力なし、1：右
    /// </summary>
    public int horizontal;

    /// <summary>
    /// -1：下、0：入力なし、1：上
    /// </summary>
    public int vertical;

    public bool jumpPressed;
    public bool lightAttackPressed;
    public bool heavyAttackPressed;
}
