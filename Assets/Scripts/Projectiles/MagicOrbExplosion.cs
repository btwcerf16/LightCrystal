using UnityEngine;

public sealed class MagicOrbExplosion : MonoBehaviour
{
    private ObjectPool<MagicOrbExplosion> _pool;

    public void Play(ObjectPool<MagicOrbExplosion> pool, Vector3 position, float radius)
    {
        _pool = pool;
        transform.position = position;
        transform.localScale = Vector3.one * radius;
        GetComponent<ParticleSystem>().Play(true);
    }

    private void OnParticleSystemStopped() => _pool.ReturnObject(this);
}
