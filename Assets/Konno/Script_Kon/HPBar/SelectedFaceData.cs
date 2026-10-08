using UnityEngine;

/// <summary>
/// キャラ選択画面で選ばれた「顔画像」を、バトルシーンへ受け渡す入れ物(static)。
/// CharacterSelectManager が決定時に書き込み、FighterPortrait(バトルシーンのHP横の顔)が読む。
/// 既存の CharacterSelectionData / FighterCharacterData には一切手を入れない。
///
/// Spriteをstaticに持つだけなので、シーンを切り替えても残る。
/// (キャラ選択を通らずにバトルシーンだけ単体再生した時は null のまま → FighterPortraitの予備画像が使われる)
/// </summary>
public static class SelectedFaceData
{
    public static Sprite Player1Face { get; private set; }
    public static Sprite Player2Face { get; private set; }

    public static void Set(Sprite player1, Sprite player2)
    {
        Player1Face = player1;
        Player2Face = player2;
    }

    public static Sprite Get(int playerNumber)
    {
        return playerNumber == 2 ? Player2Face : Player1Face;
    }

    public static void Clear()
    {
        Player1Face = null;
        Player2Face = null;
    }
}
