using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class SpearWeapon : Weapon
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private SpearProjectile _projectilePrefab;
    [SerializeField, Min(0.01f)] private float _attackRadius = 0.25f;
    [SerializeField, Min(0.01f)] private float _pointSpacing = 0.6f;
    [SerializeField, Min(0f)] private float _throwMultiplier = 1.5f;
    [SerializeField, Min(0.1f)] private float _throwSpeed = 40f;
    [SerializeField, Min(0.1f)] private float _throwRange = 9.6f;
    [SerializeField, Min(0f)] private float _sanityCost = 5f;
    [SerializeField, Min(0f)] private float _closeCooldown = 5f;
    [SerializeField, Min(0f)] private float _farCooldown = 1f;
    private ActorSanity _sanity;
    private readonly HashSet<IDamageable> _hitTargets = new();

    public override float GetRange(ActorStats owner) => owner != null ? owner.GetProjectileRange(_throwRange) : Mathf.Max(0f, _throwRange);
    public override string DisplayName => "Походное копьё";
    private float CurrentDamage => PhysicalDamage;
    public float PrimaryDamage => DamageCalculator.CalculateDamage(CurrentDamage, EDamageType.Physical, OwnerStats, null);
    public float ThrowDamage => PrimaryDamage * _throwMultiplier;
    public override string GetWeaponTooltip(bool detailed) => base.GetWeaponTooltip(detailed)
        + $"\n\nЛКМ — урон: {FormatDamage(_damage, EDamageType.Physical, detailed, true)}"
        + $"\nСкорость атаки: {FormatAttackSpeed(PrimaryAttackType, detailed)}\nЦелей за удар: {TargetLimit}"
        + $"\n\nПКМ — урон броска: {FormatDamage(_damage, EDamageType.Physical, detailed, true, _throwMultiplier)}"
        + $"\n{FormatProjectile(_throwSpeed, _throwRange, detailed)}"
        + $"\nРассудок за бросок: {_sanityCost:0.#}\nКД броска: {FormatTooltipValue(_farCooldown, GetThrowCooldown(_throwRange), detailed)} — {FormatTooltipValue(_closeCooldown, GetThrowCooldown(0f), detailed)} с"
        + (IsAvailable ? "" : "\nКопьё ещё не вернулось.");

    public override void Use() { }
    public override void Equip()
    {
        base.Equip();
        _sanity = GetComponentInParent<ActorSanity>();
        if (!_bonusApplied && OwnerStats != null && OwnerNetworkObject != null && OwnerNetworkObject.IsSpawned && OwnerNetworkObject.NetworkManager.IsServer)
        {
            OwnerStats.PhysicalDamage.AddModifier(new StatModifier(_damage, StatModifierType.Flat, this));
            _bonusApplied = true;
        }
        RefreshVisual();
    }

    public override void Drop()
    {
        CancelUse();
        if (_bonusApplied && OwnerStats != null)
            OwnerStats.PhysicalDamage.RemoveModifiersFrom(this);
        _bonusApplied = false;
        base.Drop();
        _renderer.enabled = false;
    }

    public override void SetAvailable(bool available)
    {
        base.SetAvailable(available);
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        _renderer.enabled = IsEquipped && IsAvailable;
    }

    public override bool BeginPrimaryUse()
    {
        if (IsOwnerStunned || !IsEquipped || !IsAvailable || _isAttacking)
            return false;
        _isAttacking = true;
        _hitTargets.Clear();
        SetAttackType(PrimaryAttackType);
        _animator.SetTrigger("ThrustHit");
        return true;
    }
    public override bool BeginSecondaryUse()
    {
        if (IsOwnerStunned || !IsEquipped || !IsAvailable || _isAttacking || OwnerNetworkObject == null || !OwnerNetworkObject.IsSpawned)
            return false;
        if (OwnerNetworkObject.NetworkManager.IsServer)
        {
            if (CooldownRemaining > 0f)
                return false;
            if (_sanity == null || _sanity.CurrentSanity < _sanityCost)
            {
                OwnerNetworkObject.GetComponent<NetworkPlayerWallet>().ShowHint("Недостаточно рассудка");
                return false;
            }
        }
        _isAttacking = true;
        SetAttackType(SecondaryAttackType);
        _animator.SetTrigger("Throw");
        return true;
    }

    public float GetThrowCooldown(float distance)
    {
        float cooldown = Mathf.Lerp(_closeCooldown, _farCooldown, Mathf.Clamp01(distance / _throwRange));
        return GetReducedCooldown(cooldown);
    }

    public void SetThrowCooldown(float distance)
    {
        if (OwnerNetworkObject == null || !OwnerNetworkObject.IsSpawned || !OwnerNetworkObject.NetworkManager.IsServer)
            return;
        float cooldown = GetThrowCooldown(distance);
        SetCooldown(cooldown, OwnerNetworkObject.NetworkManager.ServerTime.Time + cooldown);
        NotifyStateChanged();
    }

    public override void EndPrimaryUse() { }
    public override void EndSecondaryUse() { }
    public override void CancelUse()
    {
        _isAttacking = false;
        _animator.ResetTrigger("ThrustHit");
        _animator.ResetTrigger("Throw");
        _animator.Play("Idle", 0, 0f);
    }
    public void FinishAttack() => _isAttacking = false;

    public void StartThrust(int point)
    {
        if (!_isAttacking || IsOwnerStunned || !IsAvailable || OwnerNetworkObject == null || !OwnerNetworkObject.IsSpawned || !OwnerNetworkObject.NetworkManager.IsServer)
            return;
        Vector2 position = _attackPoint.position + _attackPoint.right * ((point - 2) * _pointSpacing);
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, _attackRadius, DamageCalculator.GetTargetMask(OwnerStats, _targetMask)))
        {
            if (_hitTargets.Count >= TargetLimit)
                break;
            if (hit.transform.root == OwnerNetworkObject.transform.root)
                continue;
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || _hitTargets.Contains(target))
                continue;
            if (ActorArsenal.DealWeaponDamage(target, CurrentDamage, EDamageType.Physical, OwnerStats, isMelee: true) > 0f)
            {
                _hitTargets.Add(target);
                NotifyMeleeHit(target, hit.ClosestPoint(position));
            }
        }
    }

    public void ThrowSpear()
    {
        if (!_isAttacking || IsOwnerStunned || !IsAvailable || OwnerNetworkObject == null || !OwnerNetworkObject.IsSpawned || !OwnerNetworkObject.NetworkManager.IsServer)
            return;
        if (CooldownRemaining > 0f)
            return;
        if (_sanity == null || !_sanity.TrySpendSanity(_sanityCost))
        {
            OwnerNetworkObject.GetComponent<NetworkPlayerWallet>().ShowHint("Недостаточно рассудка");
            return;
        }
        SpearProjectile projectile = OwnerNetworkObject.NetworkManager.GetComponent<NetworkObjectPool>()
            .GetObject(_projectilePrefab, _hand.Position, Quaternion.identity);
        projectile.SetSource(this);
        projectile.Launch(new ProjectileData
        {
            Attacker = OwnerStats, Damage = CurrentDamage * _throwMultiplier,
            DamageType = EDamageType.Physical, Direction = _hand.AimDirection,
            Speed = _throwSpeed, LifeTime = _throwRange / _throwSpeed, LayerMask = _targetMask
        });
        SetAvailable(false);
        projectile.GetComponent<NetworkObject>().Spawn();
    }
}
