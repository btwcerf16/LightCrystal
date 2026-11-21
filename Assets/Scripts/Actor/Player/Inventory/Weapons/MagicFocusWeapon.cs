using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class MagicFocusWeapon : Weapon
{
    private const float StaffWindup = 0.14f;
    private const float StaffHold = 0.19f;
    private const float StaffRecovery = 0.15f;
    private const float WandFlickDuration = 0.18f;
    private static readonly Vector3 GripPivot = new(-0.045f, 0f, 0f);

    [SerializeField] private Transform _visual;
    [SerializeField] private string _displayName;
    [SerializeField] private float _sanityBonus;
    [SerializeField] private float _castSpeedBonus;
    [SerializeField, Min(0.01f)] private float _attackInterval = 0.45f;
    [SerializeField, Min(0.1f)] private float _range = 3f;
    [SerializeField, Min(0f)] private float _primarySanityCost;
    [SerializeField, Min(0f)] private float _chargedSanityCost;
    [SerializeField] private StraightProjectile _projectilePrefab;
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 10f;
    [SerializeField, Min(0.1f)] private float _chargeTime = 1.5f;
    [SerializeField, Min(0f)] private float _chargedDamage = 8f;
    [SerializeField, Min(0.1f)] private float _chargedRange = 6f;

    private readonly List<RaycastHit2D> _beamHits = new();
    private readonly HashSet<IDamageable> _beamTargets = new();
    private EffectHandler _effects;
    private ActorSanity _sanity;
    private NetworkPlayerArsenal _networkArsenal;
    private LineRenderer _chargeFlash;
    private float _chargeElapsed;
    private float _nextAttackAt;
    private Vector3 _aimedVisualPosition;
    private Quaternion _aimedVisualRotation;
    private bool _staffShotPending;
    private float _staffMotionStartedAt = float.NegativeInfinity;
    private float _staffWindupDuration = StaffWindup;
    private float _staffRecoveryAt = float.NegativeInfinity;
    private float _staffIdleAngle = 78f;
    private float _wandFlickStartedAt = float.NegativeInfinity;
    private float _wandFlickDegrees;

    private bool UsesBeam => _projectilePrefab == null;
    public override string DisplayName => _displayName;
    public override float ChargeProgress => _isAttacking ? Mathf.Clamp01(_chargeElapsed / _chargeTime) : -1f;

    public override float GetRange(ActorStats owner) =>
        UsesBeam || owner == null ? _range : owner.GetProjectileRange(_range);

    public override string GetWeaponTooltip(bool detailed)
    {
        string tooltip = base.GetWeaponTooltip(detailed)
            + $"\n\nСкорость каста: {FormatAttackSpeed(PrimaryAttackType, detailed)}";
        string primaryDamage = FormatDamage(_damage, EDamageType.Magic, detailed);
        if (!UsesBeam)
            return tooltip + $"\nЛКМ: снаряд, {primaryDamage} маг. урона"
                + $"\n{FormatProjectile(_projectileSpeed, _range, detailed)}\nРассудок: {_primarySanityCost:0.##}";

        return tooltip + $"\nЛКМ: луч на {_range:0.##}, {primaryDamage} маг. урона всем целям на пути. Рассудок: {_primarySanityCost:0.##}"
            + $"\n\nПКМ: удерживайте {_chargeTime:0.##} с. Луч на {_chargedRange:0.##}, "
            + $"{FormatDamage(_chargedDamage, EDamageType.Magic, detailed)} маг. урона всем целям на пути. Рассудок: {_chargedSanityCost:0.##}";
    }

    private void Awake()
    {
        if (_visual != null)
        {
            _aimedVisualPosition = _visual.localPosition;
            _aimedVisualRotation = _visual.localRotation;
        }
    }

    public override void Equip()
    {
        if (IsEquipped)
            return;

        base.Equip();
        _effects = OwnerStats.GetComponent<EffectHandler>();
        _sanity = OwnerStats.GetComponent<ActorSanity>();
        _networkArsenal = OwnerStats.GetComponent<NetworkPlayerArsenal>();
        if (!IsServer)
            return;

        OwnerStats.MaxSanity.AddModifier(new StatModifier(_sanityBonus, StatModifierType.AdditivePercent, this));
        if (_castSpeedBonus != 0f)
            OwnerStats.CastSpeed.AddModifier(new StatModifier(_castSpeedBonus, StatModifierType.AdditivePercent, this));
    }

    public override void Drop()
    {
        CancelUse();
        _wandFlickStartedAt = float.NegativeInfinity;
        if (OwnerStats != null)
        {
            OwnerStats.MaxSanity.RemoveModifiersFrom(this);
            OwnerStats.CastSpeed.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    private bool CanAttack() => IsServer && IsEquipped && IsAvailable && _hand != null
        && !IsOwnerStunned && (_effects == null || (!_effects.IsDowned && !_effects.IsSilenced));

    public override bool BeginPrimaryUse()
    {
        if (!IsServer)
        {
            if (UsesBeam || !IsEquipped)
                return false;

            StartStaffMotion();
            return true;
        }

        if (!CanAttack() || _isAttacking || _staffShotPending || Time.time < _nextAttackAt
            || !CheckSanity(_primarySanityCost, spend: UsesBeam))
            return false;

        StartAttackInterval();
        if (UsesBeam)
            FireBeam(_damage, _range, false);
        else
        {
            StartStaffMotion();
            _staffShotPending = true;
        }
        return true;
    }

    public override bool BeginSecondaryUse()
    {
        if (!UsesBeam || !CanAttack() || _isAttacking || Time.time < _nextAttackAt || !CheckSanity(_chargedSanityCost, spend: false))
            return false;

        _chargeElapsed = 0f;
        _isAttacking = true;
        return true;
    }

    private void Update()
    {
        if (!UsesBeam)
        {
            UpdateStaffShot();
            return;
        }

        if (!_isAttacking)
            return;
        if (!CanAttack())
        {
            CancelUse();
            return;
        }

        _chargeElapsed += Time.deltaTime;
        if (_chargeElapsed < _chargeTime)
            return;

        CancelUse();
        if (!CheckSanity(_chargedSanityCost, spend: true))
            return;

        StartAttackInterval();
        FireBeam(_chargedDamage, _chargedRange, true);
    }

    private void StartStaffMotion()
    {
        _staffMotionStartedAt = Time.time;
        _staffWindupDuration = Mathf.Max(0.075f, StaffWindup / Mathf.Max(0.01f, OwnerStats.CastSpeed.Value));
        _staffRecoveryAt = _staffMotionStartedAt + _staffWindupDuration + StaffHold;
    }

    private void UpdateStaffShot()
    {
        if (!_staffShotPending)
            return;
        if (!CanAttack())
        {
            CancelUse();
            return;
        }
        if (Time.time - _staffMotionStartedAt < _staffWindupDuration)
            return;

        _staffShotPending = false;
        if (!CheckSanity(_primarySanityCost, spend: true))
        {
            CancelUse();
            return;
        }

        _staffRecoveryAt = Time.time + StaffHold;
        FireProjectile();
    }

    internal void PlayWandFlick(bool charged)
    {
        if (!UsesBeam || !IsEquipped)
            return;
        _wandFlickStartedAt = Time.time;
        _wandFlickDegrees = charged ? 16f : 12f;
    }

    private bool CheckSanity(float cost, bool spend)
    {
        if (cost <= 0f || (_sanity != null && (spend ? _sanity.TrySpendSanity(cost) : _sanity.CurrentSanity >= cost)))
            return true;

        OwnerNetworkObject.GetComponent<NetworkPlayerWallet>()?.ShowHint("Недостаточно рассудка");
        return false;
    }

    private void StartAttackInterval()
    {
        _nextAttackAt = Time.time + _attackInterval / Mathf.Max(0.01f, OwnerStats.CastSpeed.Value);
    }

    private void FireProjectile()
    {
        NetworkObjectPool pool = OwnerNetworkObject.NetworkManager.GetComponent<NetworkObjectPool>();
        StraightProjectile projectile = pool.GetObject(_projectilePrefab, _attackPoint.position, Quaternion.identity);
        projectile.Launch(new ProjectileData
        {
            Attacker = OwnerStats, SourceWeapon = this, Damage = _damage, DamageType = EDamageType.Magic,
            Direction = _hand.AimDirection, Speed = _projectileSpeed, LifeTime = _range / _projectileSpeed,
            LayerMask = _targetMask
        });
        projectile.GetComponent<NetworkObject>().Spawn();
    }

    private void FireBeam(float damage, float range, bool charged)
    {
        Vector2 origin = _attackPoint.position;
        Vector2 aim = _hand.AimDirection;
        Vector2 end = origin + aim * range;
        ContactFilter2D filter = new() { useTriggers = true };
        filter.SetLayerMask(DamageCalculator.GetTargetMask(OwnerStats, _targetMask));
        Physics2D.Raycast(origin, aim, filter, _beamHits, range);
        _beamHits.Sort(static (first, second) => first.distance.CompareTo(second.distance));
        _beamTargets.Clear();
        foreach (RaycastHit2D hit in _beamHits)
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.GetComponentInParent<ActorStats>() == OwnerStats)
                continue;

            IDamageable target = collider.GetComponentInParent<IDamageable>();
            if (target == null)
            {
                if (collider.isTrigger)
                    continue;

                end = hit.point;
                break;
            }

            if (target.IsDead || (target is ActorHealth health && !health.AcceptsDamageFrom(OwnerStats))
                || !_beamTargets.Add(target))
                continue;

            if (ActorArsenal.DealWeaponDamage(target, damage, EDamageType.Magic, OwnerStats) > 0f)
                NotifyHit(target, EAttackType.Magic, hit.point);
        }
        _beamHits.Clear();
        _beamTargets.Clear();
        _networkArsenal?.PlayWandBeamRpc(origin, end, charged);
    }

    private void LateUpdate()
    {
        if (!IsEquipped || _hand == null || _visual == null || _networkArsenal == null || !_networkArsenal.IsClient)
            return;

        if (!UsesBeam)
        {
            float aimX = _hand.AimDirection.x;
            if (Mathf.Abs(aimX) > 0.15f)
                _staffIdleAngle = aimX < 0f ? -78f : 78f;

            float elapsed = Time.time - _staffMotionStartedAt;
            float angle = elapsed < _staffWindupDuration
                ? Mathf.SmoothStep(_staffIdleAngle, 0f, elapsed / _staffWindupDuration)
                : Mathf.SmoothStep(0f, _staffIdleAngle, (Time.time - _staffRecoveryAt) / StaffRecovery);
            ApplyVisualAngle(angle);
            return;
        }

        float charge = _networkArsenal.WeaponCharge;
        float flick = (Time.time - _wandFlickStartedAt) / WandFlickDuration;
        float wandAngle = 0f;
        if (flick < 1f)
        {
            float side = _hand.AimDirection.x < 0f ? -1f : 1f;
            wandAngle = side * _wandFlickDegrees * Mathf.Sin(flick * Mathf.PI * 2f) * (1f - flick);
            charge = -1f;
        }
        else if (charge >= 0f)
        {
            wandAngle = Mathf.Sin(Time.time * 85f) * Mathf.Lerp(1.5f, 6f, charge);
        }
        UpdateChargeFlash(charge, ApplyVisualAngle(wandAngle));
    }

    private Quaternion ApplyVisualAngle(float angle)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        _visual.SetLocalPositionAndRotation(
            GripPivot + rotation * (_aimedVisualPosition - GripPivot), rotation * _aimedVisualRotation);
        return rotation;
    }

    private void UpdateChargeFlash(float charge, Quaternion rotation)
    {
        if (charge < 0f)
        {
            if (_chargeFlash != null)
                _chargeFlash.enabled = false;
            return;
        }

        if (_chargeFlash == null)
        {
            _chargeFlash = MagicWandBeamEffect.CreateLine(transform, "Wand charge", _networkArsenal.WandEffectMaterial, 7);
            _chargeFlash.positionCount = 4;
            _chargeFlash.loop = true;
        }

        float size = Mathf.Lerp(0.045f, 0.14f, charge);
        Vector3 tip = transform.TransformPoint(GripPivot + rotation * (_attackPoint.localPosition - GripPivot));
        _chargeFlash.SetPosition(0, tip + Vector3.up * size);
        _chargeFlash.SetPosition(1, tip + Vector3.right * size);
        _chargeFlash.SetPosition(2, tip - Vector3.up * size);
        _chargeFlash.SetPosition(3, tip - Vector3.right * size);
        _chargeFlash.widthMultiplier = Mathf.Lerp(0.025f, 0.06f, charge);
        _chargeFlash.startColor = _chargeFlash.endColor = Color.Lerp(
            new Color(0.25f, 0.73f, 0.92f, 0.5f), new Color(0.85f, 1f, 1f), charge);
        _chargeFlash.enabled = true;
    }

    public override void EndSecondaryUse() => CancelUse();
    public override void EndPrimaryUse() { }
    public override void Use() { }

    public override void CancelUse()
    {
        _isAttacking = false;
        _chargeElapsed = 0f;
        _staffShotPending = false;
        _staffMotionStartedAt = float.NegativeInfinity;
        _staffRecoveryAt = float.NegativeInfinity;
        if (_chargeFlash != null)
            _chargeFlash.enabled = false;
    }
}
