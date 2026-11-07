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
    private ActorHand _hand;
    private NetworkObject _owner;
    private bool _isAttacking;
    private readonly HashSet<IDamageable> _hitTargets = new();

    public override string DisplayName => "Походное копьё";
    private float CurrentDamage => OwnerStats != null ? OwnerStats.PhysicalDamage.Value : _damage;
    public float PrimaryDamage => DamageCalculator.CalculateDamage(CurrentDamage, EDamageType.Physical, OwnerStats, null);
    public float ThrowDamage => PrimaryDamage * _throwMultiplier;
    public override string TooltipDescription => base.TooltipDescription
        + $"\n\nУрон ЛКМ: {PrimaryDamage:0.#}\nУрон броска: {ThrowDamage:0.#}\nЦелей за удар: {TargetLimit}"
        + $"\nРассудок за бросок: {_sanityCost:0.#}\nКД броска: {GetThrowCooldown(_throwRange):0.#}–{GetThrowCooldown(0f):0.#} с"
        + (IsAvailable ? "" : "\nКопьё ещё не вернулось.");

    public override void Use() { }
    public override void Equip()
    {
        base.Equip();
        _hand = GetComponentInParent<ActorHand>();
        _sanity = GetComponentInParent<ActorSanity>();
        _owner = GetComponentInParent<NetworkObject>();
        if (!_bonusApplied && OwnerStats != null && _owner != null && _owner.IsSpawned && _owner.NetworkManager.IsServer)
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
        if (_hand != null)
            _hand.VisualRenderer.enabled = true;
    }

    public override void SetAvailable(bool available)
    {
        base.SetAvailable(available);
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        _renderer.enabled = IsEquipped && IsAvailable;
        if (_hand != null && IsEquipped)
            _hand.VisualRenderer.enabled = !IsAvailable;
    }

    public override void BeginPrimaryUse()
    {
        if (IsOwnerStunned || !IsEquipped || !IsAvailable || _isAttacking)
            return;
        _isAttacking = true;
        _hitTargets.Clear();
        _animator.SetTrigger("ThrustHit");
    }
    public override bool BeginSecondaryUse()
    {
        if (IsOwnerStunned || !IsEquipped || !IsAvailable || _isAttacking || _owner == null || !_owner.IsSpawned)
            return false;
        if (_owner.NetworkManager.IsServer)
        {
            if (CooldownRemaining > 0f)
                return false;
            if (_sanity == null || _sanity.CurrentSanity < _sanityCost)
            {
                _owner.GetComponent<NetworkPlayerWallet>().ShowHint("Недостаточно рассудка");
                return false;
            }
        }
        _isAttacking = true;
        _animator.SetTrigger("Throw");
        return true;
    }

    public float GetThrowCooldown(float distance)
    {
        float cooldown = Mathf.Lerp(_closeCooldown, _farCooldown, Mathf.Clamp01(distance / _throwRange));
        float cdr = OwnerStats != null ? Mathf.Clamp(OwnerStats.CooldownReduction.Value, 0f, 0.9f) : 0f;
        return cooldown * (1f - cdr);
    }

    public void SetThrowCooldown(float distance)
    {
        if (_owner == null || !_owner.IsSpawned || !_owner.NetworkManager.IsServer)
            return;
        float cooldown = GetThrowCooldown(distance);
        SetCooldown(cooldown, _owner.NetworkManager.ServerTime.Time + cooldown);
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
        if (!_isAttacking || IsOwnerStunned || !IsAvailable || _owner == null || !_owner.IsSpawned || !_owner.NetworkManager.IsServer)
            return;
        Vector2 position = _attackPoint.position + _attackPoint.right * ((point - 2) * _pointSpacing);
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, _attackRadius))
        {
            if (_hitTargets.Count >= TargetLimit)
                break;
            if (hit.transform.root == transform.root)
                continue;
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || _hitTargets.Contains(target))
                continue;
            if (DamageCalculator.DealCriticalDamage(target, CurrentDamage, EDamageType.Physical, OwnerStats) > 0f)
            {
                _hitTargets.Add(target);
                NotifyMeleeHit(target);
            }
        }
    }

    public void ThrowSpear()
    {
        if (!_isAttacking || IsOwnerStunned || !IsAvailable || _owner == null || !_owner.IsSpawned || !_owner.NetworkManager.IsServer)
            return;
        if (CooldownRemaining > 0f)
            return;
        if (_sanity == null || !_sanity.TrySpendSanity(_sanityCost))
        {
            _owner.GetComponent<NetworkPlayerWallet>().ShowHint("Недостаточно рассудка");
            return;
        }
        SpearProjectile projectile = _owner.NetworkManager.GetComponent<NetworkObjectPool>()
            .GetObject(_projectilePrefab, _hand.Position, Quaternion.identity);
        projectile.SetSource(this);
        projectile.Launch(new ProjectileData
        {
            Attacker = OwnerStats, Damage = CurrentDamage * _throwMultiplier,
            DamageType = EDamageType.Physical, Direction = GetProjectileDirection(_hand.AimDirection),
            Speed = _throwSpeed, LifeTime = _throwRange / _throwSpeed, LayerMask = ~0
        });
        SetAvailable(false);
        projectile.GetComponent<NetworkObject>().Spawn();
    }
}
