using UnityEngine;

public sealed class EffectAutoDestroy : MonoBehaviour
{
    /// <summary>
    /// Animation Event‚©‚çŒÄ‚ÔB
    /// </summary>
    public void DestroyEffect()
    {
        Destroy(gameObject);
    }
}
