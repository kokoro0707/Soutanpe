/// <summary>
/// キャラクターに入力情報を渡すための共通インターフェース。
/// </summary>
public interface IFighterInputSource
{
    FighterInputData ReadInput();
}
