using Unity.Netcode;
using UnityEngine;

public sealed class GlassShardProjectile : Projectile
{
    [SerializeField] private DamageTrap _trapPrefab;
    [SerializeField] private EffectData _slowEffect;
    [SerializeField, Min(0f)] private float _trapDamage = 2f;
    [SerializeField, Min(0.1f)] private float _trapLifeTime = 10f;
    [SerializeField, Min(0.01f)] private float _trapTriggerInterval = 0.5f;
    [SerializeField] private LayerMask _trapTargetMask;

    protected override bool CanHit(Collider2D targetCollider)
    {
        return base.CanHit(targetCollider) && ((targetCollider.GetComponentInParent<Enemy>() != null || targetCollider.GetComponentInParent<EnchantRune>() != null) || (!targetCollider.isTrigger && targetCollider.GetComponentInParent<IDamageable>() == null));
    }

    public override void OnHit(Collider2D targetCollider)
    {
        if (!IsFlying || !CanHit(targetCollider))
            return;
        IDamageable caughtTarget = targetCollider.GetComponentInParent<IDamageable>();
        if (caughtTarget != null)
        {
            float dealtDamage = DamageCalculator.DealCriticalDamage(caughtTarget, Data.Damage, Data.DamageType, Data.Attacker);
            if (dealtDamage > 0f && Data.SourceWeapon != null)
                Data.SourceWeapon.NotifyHit(caughtTarget);
        }
        LeaveTrap();
    }

    protected override void OnLifetimeExpired() => LeaveTrap();

    private void LeaveTrap()
    {
        if (!IsFlying)
            return;
        StopFlight();
        NetworkObjectPool projectilePool = GetComponent<NetworkObject>().NetworkManager.GetComponent<NetworkObjectPool>();
        DamageTrap glassTrap = projectilePool.GetObject(_trapPrefab, transform.position, Quaternion.identity);
        glassTrap.Initialize(new TrapData
        {
            Owner = Data.Attacker, SourceWeapon = Data.SourceWeapon, EffectOnCaught = _slowEffect,
            Damage = _trapDamage, DamageType = Data.DamageType, TargetsLayerMask = _trapTargetMask,
            LifeTime = _trapLifeTime, TriggerInterval = _trapTriggerInterval, OperationsCount = 0
        });
        glassTrap.GetComponent<NetworkObject>().Spawn();
        Finish();
    }
}
