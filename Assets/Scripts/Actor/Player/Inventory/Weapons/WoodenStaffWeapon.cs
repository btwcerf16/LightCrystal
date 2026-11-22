using Unity.Netcode;
using UnityEngine;

public sealed class WoodenStaffWeapon : MagicWeapon
{
    private const float Hold = 0.08f;
    private const float Recovery = 0.13f;
    private const float IdleAngle = 78f;

    [SerializeField, Min(0.01f)] private float _castTime = 0.22f;
    [SerializeField] private StraightProjectile _projectilePrefab;
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 10f;

    private float _windupAt;
    private float _holdDuration;
    private float _recoveryDuration;
    private float _fireAt = float.NegativeInfinity;
    private float _recoverAt = float.NegativeInfinity;

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
        }

        float speed = CastSpeed;
        _windupAt = Time.time;
        _fireAt = _windupAt + _castTime / speed;
        _holdDuration = Hold / speed;
        _recoveryDuration = Recovery / speed;
        _recoverAt = _fireAt + _holdDuration;
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
        _recoverAt = Time.time + _holdDuration;
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

    private void LateUpdate()
    {
        if (!CanAnimate)
            return;

        float now = Time.time;
        float angle = IdleAngle;
        if (now < _fireAt)
            angle = Mathf.SmoothStep(IdleAngle, 0f, (now - _windupAt) / (_fireAt - _windupAt));
        else if (now < _recoverAt + _recoveryDuration)
            angle = Mathf.SmoothStep(0f, IdleAngle, (now - _recoverAt) / _recoveryDuration);
        _grip.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public override void CancelUse()
    {
        _isAttacking = false;
        _fireAt = _recoverAt = float.NegativeInfinity;
    }
}
