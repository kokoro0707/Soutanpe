using UnityEngine;

public sealed class ParticleEffectAutoDestroy : MonoBehaviour
{
    [SerializeField]
    private float lifeTime = 1f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
