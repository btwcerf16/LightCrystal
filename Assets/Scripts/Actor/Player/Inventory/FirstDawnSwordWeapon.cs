using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class FirstDawnSwordWeapon : Weapon
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private TrailRenderer _trail;
    [SerializeField] private DawnWaveProjectile _wavePrefab;
    [SerializeField] private EffectData _stun;
    [SerializeField] private float _waveDamage = 25f;
    [SerializeField] private float _sanityCost = 35f;
    [SerializeField] private float _waveSpeed = 16f;
    [SerializeField] private float _waveRange = 6f;
    [SerializeField] private float _attackRadius = 0.45f;
    private ActorHand _hand;
    private ActorSanity _sanity;
    private EffectHandler _effects;
    private NetworkObject _owner;
    private bool _isAttacking;
    private bool _isWave;
    private bool _waveReleased;
    private readonly HashSet<IDamageable> _hitTargets = new();

    public override string DisplayName => "Осколок первого рассвета";
    private float CurrentDamage => OwnerStats != null ? OwnerStats.PhysicalDamage.Value : _damage;
    public float HitDamage => DamageCalculator.CalculateDamage(CurrentDamage, EDamageType.Physical, OwnerStats, null);
    public float WaveDamage => DamageCalculator.CalculateDamage(_waveDamage, EDamageType.Magic, OwnerStats, null);
    public float AbilityCooldown => BaseCooldown * (1f - (OwnerStats != null ? Mathf.Clamp(OwnerStats.CooldownReduction.Value, 0f, 0.9f) : 0f));
    public override string TooltipDescription => base.TooltipDescription
        + $"\n\nУрон клинка: {HitDamage:0.#}\nУрон волны: {WaveDamage:0.#} маг.\nЦелей за удар: {TargetLimit}"
        + $"\nСтан: {_stun.EffectDuration:0.#} с\nРассудок: {_sanityCost:0.#}\nКулдаун: {AbilityCooldown:0.#} с";

    public override void Equip()
    {
        base.Equip();
        _hand = GetComponentInParent<ActorHand>();
        _sanity = GetComponentInParent<ActorSanity>();
        _effects = GetComponentInParent<EffectHandler>();
        _owner = GetComponentInParent<NetworkObject>();
        if (!_bonusApplied && OwnerStats != null && _owner != null && _owner.IsSpawned && _owner.NetworkManager.IsServer)
        {
            OwnerStats.PhysicalDamage.AddModifier(new StatModifier(_damage, StatModifierType.Flat, this));
            _bonusApplied = true;
        }
        if (_hand != null)
            _hand.VisualRenderer.enabled = false;
        _renderer.enabled = true;
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

    private void BeginAttack(bool wave)
    {
        _isAttacking = true;
        _isWave = wave;
        _waveReleased = false;
        _hitTargets.Clear();
        _animator.SetTrigger(wave ? "Wave" : "Swing");
    }

    public override void BeginPrimaryUse()
    {
        if (IsEquipped && IsAvailable && !IsOwnerStunned && !_isAttacking)
            BeginAttack(false);
    }

    public override bool BeginSecondaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking || _owner == null || !_owner.IsSpawned)
            return false;
        if (!_owner.NetworkManager.IsServer)
        {
            BeginAttack(true);
            return true;
        }
        if (CooldownRemaining > 0f)
            return false;
        string error = _effects != null && _effects.IsSilenced ? "Вы под действием безмолвия"
            : _sanity == null || _sanity.CurrentSanity < _sanityCost ? "Недостаточно рассудка" : null;
        if (error != null)
        {
            _owner.GetComponent<NetworkPlayerWallet>().ShowHint(error);
            return false;
        }
        return TryUse();
    }

    public override void Use()
    {
        _sanity.TrySpendSanity(_sanityCost);
        BeginAttack(true);
        NotifyStateChanged();
    }

    private void LateUpdate()
    {
        if (_hand == null)
            return;
        Vector3 scale = transform.localScale;
        scale.y = Mathf.Abs(scale.y) * (_hand.AimDirection.x < 0f ? -1f : 1f);
        transform.localScale = scale;
    }

    private bool CanStrike => _isAttacking && IsEquipped && !IsOwnerStunned
        && _owner != null && _owner.IsSpawned && _owner.NetworkManager.IsServer;

    public void Strike()
    {
        if (!CanStrike || _isWave)
            return;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(_attackPoint.position, _attackRadius))
        {
            if (_hitTargets.Count >= TargetLimit)
                break;
            if (hit.transform.root == transform.root)
                continue;
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || _hitTargets.Contains(target))
                continue;
            if (DamageCalculator.DealCriticalDamage(target, CurrentDamage, EDamageType.Physical, OwnerStats) <= 0f)
                continue;
            _hitTargets.Add(target);
            NotifyMeleeHit(target);
        }
    }

    public void ReleaseWave()
    {
        if (!CanStrike || !_isWave || _waveReleased)
            return;
        _waveReleased = true;
        DawnWaveProjectile wave = _owner.NetworkManager.GetComponent<NetworkObjectPool>()
            .GetObject(_wavePrefab, _hand.Position, Quaternion.identity);
        wave.SetSource(this, _stun, TargetLimit);
        wave.Launch(new ProjectileData
        {
            Attacker = OwnerStats, Damage = _waveDamage, DamageType = EDamageType.Magic,
            Direction = GetProjectileDirection(_hand.AimDirection), Speed = _waveSpeed, LifeTime = _waveRange / _waveSpeed, LayerMask = ~0
        });
        wave.GetComponent<NetworkObject>().Spawn();
    }

    public void StartTrail()
    {
        _trail.Clear();
        _trail.emitting = true;
    }
    public void FinishAttack()
    {
        _isAttacking = false;
        _trail.emitting = false;
    }
    public override void EndPrimaryUse() { }
    public override void EndSecondaryUse() { }
    public override void CancelUse()
    {
        FinishAttack();
        _animator.ResetTrigger("Swing");
        _animator.ResetTrigger("Wave");
        _animator.Play("Idle", 0, 0f);
    }
}
