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
    private ActorSanity _sanity;
    private EffectHandler _effects;
    private bool _isWave;
    private bool IsCastBlocked => IsOwnerStunned || IsOwnerSilenced;
    private bool _waveReleased;
    private readonly HashSet<IDamageable> _hitTargets = new();

    public override float GetRange(ActorStats owner) => owner != null ? owner.GetProjectileRange(_waveRange) : Mathf.Max(0f, _waveRange);
    public override string DisplayName => "Осколок первого рассвета";
    private float CurrentDamage => PhysicalDamage;
    public float HitDamage => DamageCalculator.CalculateDamage(CurrentDamage, EDamageType.Physical, OwnerStats, null);
    public float WaveDamage => DamageCalculator.CalculateDamage(_waveDamage, EDamageType.Magic, OwnerStats, null);
    public float AbilityCooldown => GetReducedCooldown(BaseCooldown);
    public override string GetWeaponTooltip(bool detailed) => base.GetWeaponTooltip(detailed)
        + $"\n\nЛКМ — клинок: {FormatDamage(_damage, EDamageType.Physical, detailed, true)} физ. урона"
        + $"\nСкорость атаки: {FormatAttackSpeed(PrimaryAttackType, detailed)}\nЦелей ЛКМ: {SplashTargetLimit} · ПКМ: {TargetLimit}"
        + $"\n\nПКМ — волна: {FormatDamage(_waveDamage, EDamageType.Magic, detailed)} маг. урона"
        + $"\nСкорость каста: {FormatAttackSpeed(SecondaryAttackType, detailed)}\n{FormatProjectile(_waveSpeed, _waveRange, detailed)}"
        + $"\nСтан: {_stun.EffectDuration:0.#} с\nРассудок: {_sanityCost:0.#}\nКулдаун: {FormatTooltipValue(BaseCooldown, AbilityCooldown, detailed)} с";

    public override void Equip()
    {
        base.Equip();
        _sanity = GetComponentInParent<ActorSanity>();
        _effects = GetComponentInParent<EffectHandler>();
        if (!_bonusApplied && OwnerStats != null && OwnerNetworkObject != null && OwnerNetworkObject.IsSpawned && OwnerNetworkObject.NetworkManager.IsServer)
        {
            OwnerStats.PhysicalDamage.AddModifier(new StatModifier(_damage, StatModifierType.Flat, this));
            _bonusApplied = true;
        }
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
    }

    private void BeginAttack(bool wave)
    {
        SetAttackType(wave ? SecondaryAttackType : PrimaryAttackType);
        _isAttacking = true;
        _isWave = wave;
        _waveReleased = false;
        _hitTargets.Clear();
        _animator.SetTrigger(wave ? "Wave" : "Swing");
    }

    public override bool BeginPrimaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking)
            return false;
        BeginAttack(false);
        return true;
    }

    public override bool BeginSecondaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking || OwnerNetworkObject == null || !OwnerNetworkObject.IsSpawned)
            return false;
        if (!OwnerNetworkObject.NetworkManager.IsServer)
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
            OwnerNetworkObject.GetComponent<NetworkPlayerWallet>().ShowHint(error);
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

    private bool CanStrike => _isAttacking && IsEquipped && !IsOwnerStunned
        && OwnerNetworkObject != null && OwnerNetworkObject.IsSpawned && OwnerNetworkObject.NetworkManager.IsServer;

    public void Strike()
    {
        if (!CanStrike || _isWave)
            return;
        int targetLimit = SplashTargetLimit;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(_attackPoint.position, _attackRadius, DamageCalculator.GetTargetMask(OwnerStats, _targetMask)))
        {
            if (_hitTargets.Count >= targetLimit)
                break;
            if (hit.transform.root == OwnerNetworkObject.transform.root)
                continue;
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || _hitTargets.Contains(target))
                continue;
            if (DamageCalculator.DealCriticalDamage(target, CurrentDamage, EDamageType.Physical, OwnerStats, isMelee: true) <= 0f)
                continue;
            _hitTargets.Add(target);
            NotifyMeleeHit(target, hit.ClosestPoint(_attackPoint.position));
        }
    }

    private void Update()
    {
        if (_isAttacking && _isWave && !_waveReleased && IsCastBlocked)
            CancelUse();
    }

    public void ReleaseWave()
    {
        if (!CanStrike || !_isWave || _waveReleased || IsOwnerSilenced)
            return;
        _waveReleased = true;
        DawnWaveProjectile wave = OwnerNetworkObject.NetworkManager.GetComponent<NetworkObjectPool>()
            .GetObject(_wavePrefab, _hand.Position, Quaternion.identity);
        wave.SetSource(this, _stun, TargetLimit);
        wave.Launch(new ProjectileData
        {
            Attacker = OwnerStats, Damage = _waveDamage, DamageType = EDamageType.Magic,
            Direction = _hand.AimDirection, Speed = _waveSpeed, LifeTime = _waveRange / _waveSpeed, LayerMask = _targetMask
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
