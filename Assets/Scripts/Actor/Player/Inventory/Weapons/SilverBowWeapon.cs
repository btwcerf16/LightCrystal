using Unity.Netcode;
using UnityEngine;

public sealed class SilverBowWeapon : Bow
{
    [SerializeField] private Projectile _arrowPrefab;
    [SerializeField, Min(0.1f)] private float _arrowSpeed = 23f;
    [SerializeField, Min(0.1f)] private float _arrowRange = 10f;

    public override string DisplayName => "Серебряный лук";

    public override float GetRange(ActorStats owner) => owner != null ? owner.GetProjectileRange(_arrowRange) : _arrowRange;

    public override string GetWeaponTooltip(bool detailed) => Description
        + $"\n\nЛКМ: {FormatDamage(_damage, EDamageType.Physical, detailed)} физ. урона. Целей за выстрел: {TargetLimit}."
        + $"\n{FormatProjectile(_arrowSpeed, _arrowRange, detailed)}";

    protected override void ShootArrow()
    {
        NetworkObjectPool pool = OwnerStats.GetComponent<NetworkObject>().NetworkManager.GetComponent<NetworkObjectPool>();
        Projectile arrow = pool.GetObject(_arrowPrefab, _attackPoint.position, Quaternion.identity);
        arrow.Launch(new ProjectileData
        {
            Attacker = OwnerStats, SourceWeapon = this, Damage = _damage, DamageType = EDamageType.Physical,
            Direction = _hand.AimDirection, Speed = _arrowSpeed, LifeTime = _arrowRange / _arrowSpeed,
            LayerMask = _targetMask, IsArrow = true
        });
        arrow.GetComponent<NetworkObject>().Spawn();
    }
}
