using UnityEngine;

public sealed class FighterAnimationEventRelay : MonoBehaviour
{
    private FighterMoveController moveController;

    private void Awake()
    {
        moveController =
            GetComponentInParent<FighterMoveController>();
    }

    /// <summary>
    /// Animation Event‚©‚çŒÄ‚Ô
    /// </summary>
    public void EndMoveEffect()
    {
        if (moveController == null)
        {
            return;
        }

        moveController.EndMoveEffect();
    }
}
