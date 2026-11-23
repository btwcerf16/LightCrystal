using Unity.Netcode;
using UnityEngine;

public sealed class WoodenStaffWeapon : MagicWeapon
{
    [SerializeField, Min(0.01f)] private float _castTime = 0.22f;
    [SerializeField] private StraightProjectile _projectilePrefab;
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 10f;

    private float _windupAt;
    private float _fireAt = float.NegativeInfinity;

    public override float ChargeProgress => _isAttacking
        ? Mathf.Clamp01((Time.time - _windupAt) / (_fireAt - _windupAt))
        : -1f;

    public override float GetRange(ActorStats owner) => owner != null ? owner.GetProjectileRange(_range) : _range;

    public override string GetWeaponTooltip(bool detailed) => base.GetWeaponTooltip(detailed)
        + $"\n\nСкорость каста: {FormatAttackSpeed(PrimaryAttackType, detailed)}"
        + $"\nЛКМ: снаряд, {FormatDamage(_damage, EDamageType.Magic, detailed)} маг. урона"
        + $"\nВремя каста: {FormatTooltipValue(_castTime, _castTime / CastSpeed, detailed)} с"
        + $"\n{FormatProjectile(_projectileSpeed, _range, detailed)}\nРассудок: {_primarySanityCost:0.##}";

    public override bool BeginPrimaryUse()
    {
        if (!IsEquipped)
            return false;
        if (IsServer)
        {
            if (!CanStartAttack || !CheckSanity(_primarySanityCost, spend: false))
                return false;

            _isAttacking = true;
            _windupAt = Time.time;
            _fireAt = _windupAt + _castTime / CastSpeed;
        }

        _animator.Play("Fire", 0, 0f);
        return true;
    }

    private void Update()
    {
        if (!_isAttacking)
            return;
        if (!CanAttack)
        {
            CancelUse();
            return;
        }
        if (Time.time < _fireAt)
            return;
        if (!CheckSanity(_primarySanityCost, spend: true))
        {
            CancelUse();
            return;
        }

        _isAttacking = false;
        var pool = OwnerNetworkObject.NetworkManager.GetComponent<NetworkObjectPool>();
        var projectile = pool.GetObject(_projectilePrefab, _attackPoint.position, Quaternion.identity);
        projectile.Launch(new ProjectileData
        {
            Attacker = OwnerStats, SourceWeapon = this, Damage = _damage, DamageType = EDamageType.Magic,
            Direction = _hand.AimDirection, Speed = _projectileSpeed, LifeTime = _range / _projectileSpeed,
            LayerMask = _targetMask
        });
        projectile.GetComponent<NetworkObject>().Spawn();
    }

}
