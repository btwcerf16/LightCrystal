using Unity.Netcode;
using UnityEngine;

public sealed class BoneCrossbowWeapon : Bow
{
    [SerializeField] private Projectile _bonePrefab;
    [SerializeField, Min(0.1f)] private float _boneSpeed = 12f;
    [SerializeField, Min(0.1f)] private float _boneRange = 9f;
    [SerializeField] private LayerMask _targetMask;

    protected override bool FiresOnRelease => false;
    public override string DisplayName => "Костяной арбалет";

    public override float GetRange(ActorStats owner) => owner != null ? owner.GetProjectileRange(_boneRange) : _boneRange;

    public override string GetWeaponTooltip(bool detailed) => Description
        + $"\n\nЛКМ: заряди арбалет, отпусти и нажми снова. Кость наносит {FormatDamage(_damage, EDamageType.Physical, detailed)} физ. урона."
        + $"\n{FormatProjectile(_boneSpeed, _boneRange, detailed)}"
        + "\nПолный заряд сохраняется при смене оружия.";

    protected override void ShootArrow()
    {
        NetworkObjectPool pool = OwnerStats.GetComponent<NetworkObject>().NetworkManager.GetComponent<NetworkObjectPool>();
        Projectile bone = pool.GetObject(_bonePrefab, _attackPoint.position, Quaternion.identity);
        bone.Launch(new ProjectileData
        {
            Attacker = OwnerStats, SourceWeapon = this, Damage = _damage, DamageType = EDamageType.Physical,
            Direction = _hand.AimDirection, Speed = _boneSpeed, LifeTime = _boneRange / _boneSpeed,
            LayerMask = _targetMask
        });
        bone.GetComponent<NetworkObject>().Spawn();
    }
}
