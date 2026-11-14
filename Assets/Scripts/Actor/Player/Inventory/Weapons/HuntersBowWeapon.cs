using Unity.Netcode;
using UnityEngine;

public sealed class HuntersBowWeapon : Bow
{
    [SerializeField] private HunterArrowProjectile _arrowPrefab;
    [SerializeField, Min(0.1f)] private float _arrowSpeed = 20f;
    [SerializeField, Min(0.1f)] private float _arrowRange = 9f;
    [SerializeField] private LayerMask _targetMask;

    public override string DisplayName => "Набор охотника";

    public override float GetRange(ActorStats owner) => owner != null ? owner.GetProjectileRange(_arrowRange) : _arrowRange;

    public override string GetWeaponTooltip(bool detailed) => Description
        + $"\n\nЛКМ: {FormatDamage(_damage, EDamageType.Physical, detailed)} физ. урона при полном натяжении."
        + $"\n{FormatProjectile(_arrowSpeed, _arrowRange, detailed)}"
        + "\nШанс оставить шипы при добивании равен шансу крита. Шипы живут до 8 с и обездвиживают на 3 с."
        + "\nЗа каждого пойманного врага: +10% к скорости снарядов на 8 с.";

    protected override void ShootArrow()
    {
        NetworkObjectPool pool = OwnerStats.GetComponent<NetworkObject>().NetworkManager.GetComponent<NetworkObjectPool>();
        HunterArrowProjectile arrow = pool.GetObject(_arrowPrefab, _attackPoint.position, Quaternion.identity);
        arrow.Launch(new ProjectileData
        {
            Attacker = OwnerStats, SourceWeapon = this, Damage = _damage, DamageType = EDamageType.Physical,
            Direction = _hand.AimDirection, Speed = _arrowSpeed, LifeTime = _arrowRange / _arrowSpeed,
            LayerMask = _targetMask, IsArrow = true
        });
        arrow.GetComponent<NetworkObject>().Spawn();
    }
}
