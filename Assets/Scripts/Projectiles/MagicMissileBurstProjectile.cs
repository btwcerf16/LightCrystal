using Unity.Netcode;
using UnityEngine;

public sealed class MagicMissileBurstProjectile : Projectile
{
    [SerializeField] private MagicMissleProjectile _missilePrefab;
    [SerializeField, Min(0.1f)] private float _missileSpeed = 12f;
    [SerializeField, Min(0.1f)] private float _missileRange = 12f;
    private Weapon _source;

    public void SetSource(Weapon source) => _source = source;

    protected override bool CanHit(Collider2D collider)
    {
        return base.CanHit(collider) && (collider.GetComponentInParent<Enemy>() != null
            || (!collider.isTrigger && collider.GetComponentInParent<IDamageable>() == null));
    }

    public override void OnHit(Collider2D collider)
    {
        if (IsFlying && CanHit(collider))
            Burst();
    }

    protected override void OnLifetimeExpired() => Burst();

    private void Burst()
    {
        if (!IsFlying)
            return;
        StopFlight();
        NetworkObjectPool pool = GetComponent<NetworkObject>().NetworkManager.GetComponent<NetworkObjectPool>();
        for (int i = 0; i < 8; i++)
        {
            MagicMissleProjectile missile = pool.GetObject(_missilePrefab, transform.position, Quaternion.identity);
            missile.SetSource(_source);
            missile.Launch(new ProjectileData
            {
                Attacker = Data.Attacker, Damage = Data.Damage / 3f, DamageType = Data.DamageType,
                Direction = Quaternion.Euler(0f, 0f, i * 45f) * Vector2.right,
                Speed = _missileSpeed, LifeTime = _missileRange / _missileSpeed, LayerMask = Data.LayerMask
            });
            missile.GetComponent<NetworkObject>().Spawn();
        }
        Finish();
    }

    protected override void OnDisable()
    {
        _source = null;
        base.OnDisable();
    }
}
