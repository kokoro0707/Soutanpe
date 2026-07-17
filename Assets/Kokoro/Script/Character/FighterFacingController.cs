using UnityEngine;

/// <summary>
/// 対戦相手の位置に合わせてキャラクターを自動で振り向かせる。
/// </summary>
public sealed class FighterFacingController : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("対戦相手のルートオブジェクト")]
    [SerializeField]
    private Transform opponent;

    [Tooltip("Spriteやモデルを表示している子オブジェクト")]
    [SerializeField]
    private Transform visualRoot;

    [Header("画像の初期方向")]
    [Tooltip("元画像が右向きの場合はON")]
    [SerializeField]
    private bool visualFacesRightByDefault = true;

    private float visualScaleX = 1f;

    /// <summary>
    /// 右向きなら1、左向きなら-1。
    /// </summary>
    public int FacingDirection { get; private set; } = 1;

    private void Awake()
    {
        if (visualRoot == null)
        {
            Debug.LogError(
                $"{name}のVisual Rootが設定されていません。",
                this
            );

            return;
        }

        visualScaleX =
            Mathf.Abs(visualRoot.localScale.x);
    }

    private void Start()
    {
        RefreshFacing(true);
    }

    /// <summary>
    /// 相手の位置を確認して向きを更新する。
    /// </summary>
    public void RefreshFacing(bool canTurn)
    {
        if (!canTurn || opponent == null)
        {
            return;
        }

        float difference =
            opponent.position.x - transform.position.x;

        // ほぼ同じ位置の場合は向きを変更しない
        if (Mathf.Abs(difference) < 0.001f)
        {
            return;
        }

        int nextDirection =
            difference > 0f ? 1 : -1;

        if (nextDirection == FacingDirection)
        {
            return;
        }

        FacingDirection = nextDirection;

        ApplyVisualDirection();
    }

    /// <summary>
    /// 見た目だけを左右反転させる。
    /// </summary>
    private void ApplyVisualDirection()
    {
        if (visualRoot == null)
        {
            return;
        }

        int visualDirection =
            visualFacesRightByDefault
                ? FacingDirection
                : -FacingDirection;

        Vector3 scale = visualRoot.localScale;

        scale.x =
            visualScaleX * visualDirection;

        visualRoot.localScale = scale;
    }

    /// <summary>
    /// 生成後などに対戦相手を設定するときに使用する。
    /// </summary>
    public void SetOpponent(Transform newOpponent)
    {
        opponent = newOpponent;

        RefreshFacing(true);
    }
}
