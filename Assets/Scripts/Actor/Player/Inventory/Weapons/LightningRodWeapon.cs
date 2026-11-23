using System.Collections.Generic;
using UnityEngine;

public sealed class LightningRodWeapon : MagicWeapon
{
    [SerializeField] private string _chainEffectId = "lightning-chain";
    [SerializeField] private string _stormEffectId = "lightning-storm";
    [SerializeField] private float _lifeSteal = 0.1f;
    [SerializeField, Min(0.01f)] private float _castTime = 0.15f;
    [SerializeField] private float _bounceRadius = 2.5f;
    [SerializeField] private int _baseBounces = 3;
    [SerializeField] private float _stormCastTime = 0.85f;
    [SerializeField] private float _stormRadius = 2.75f;
    [SerializeField] private float _stormDamage = 25f;
    [SerializeField] private float _healPerEnemy = 5f;

    private readonly List<RaycastHit2D> _hits = new();
    private readonly List<Collider2D> _overlaps = new();
    private readonly HashSet<ActorHealth> _struck = new();
    private readonly List<Vector2> _path = new();
    private float _castEndsAt;
    private float _castDuration;
    private bool _storm;

    public override float GetRange(ActorStats owner) => _range;
    public override float ChargeProgress => _isAttacking
        ? 1f - Mathf.Clamp01((_castEndsAt - Time.time) / _castDuration)
        : -1f;

    public override string GetWeaponTooltip(bool detailed) => base.GetWeaponTooltip(detailed)
        + $"\n\nЛКМ: {FormatDamage(_damage, EDamageType.Magic, detailed)} маг. урона, {_baseBounces} отскока + пронзания."
        + $" Время каста: {FormatTooltipValue(_castTime, _castTime / CastSpeed, detailed)} с. Самоотскок наносит половину урона. Рассудок: {_primarySanityCost:0.#}."
        + $"\nПКМ: удерживайте {_stormCastTime / CastSpeed:0.##} с. Удар на {_stormDamage:0.#} маг. урона по себе и врагам в радиусе {_stormRadius:0.##}."
        + $" Лечение: {_healPerEnemy:0.#} за задетого врага. КД: {GetReducedCooldown(BaseCooldown):0.#} с. Самоурон оставляет минимум 0,1 HP.";

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (IsServer)
            OwnerStats.MagicLifeSteal.AddModifier(new StatModifier(_lifeSteal, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (IsServer)
            OwnerStats.MagicLifeSteal.RemoveModifiersFrom(this);
        base.Drop();
    }

    public override bool BeginPrimaryUse()
    {
        if (!IsEquipped)
            return false;
        if (IsServer)
        {
            if (!CanStartAttack || !CheckSanity(_primarySanityCost, spend: false))
                return false;
            BeginCast(_castTime, storm: false);
        }
        else
        {
            _storm = false;
        }
        _animator.Play("Fire", 0, 0f);
        return true;
    }

    public override bool BeginSecondaryUse()
    {
        if (!IsEquipped)
            return false;

        if (IsServer)
        {
            if (!CanStartAttack || CooldownRemaining > 0f)
                return false;
            BeginCast(_stormCastTime, storm: true);
        }
        else
        {
            _storm = true;
        }

        _animator.Play("Charge", 0, 0f);
        return true;
    }

    private void BeginCast(float duration, bool storm)
    {
        _castDuration = duration / CastSpeed;
        _castEndsAt = Time.time + _castDuration;
        _storm = storm;
        _isAttacking = true;
    }

    public override void EndSecondaryUse()
    {
        if (_storm)
            CancelUse();
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
        if (Time.time < _castEndsAt)
            return;

        _isAttacking = false;
        if (_storm)
            StrikeStorm();
        else if (CheckSanity(_primarySanityCost, spend: true))
            FireChain();
    }

    private ContactFilter2D TargetFilter()
    {
        ContactFilter2D filter = new() { useTriggers = true };
        filter.SetLayerMask(DamageCalculator.GetTargetMask(OwnerStats, _targetMask));
        return filter;
    }

    private ActorHealth GetEnemy(Collider2D collider)
    {
        ActorHealth target = collider.GetComponentInParent<ActorHealth>();
        return target != null && !target.IsDead && target.TryGetComponent<Enemy>(out _)
            && target.AcceptsDamageFrom(OwnerStats) ? target : null;
    }

    private void FireChain()
    {
        ActorHealth owner = OwnerStats.GetComponent<ActorHealth>();
        Vector2 origin = _attackPoint.position;
        Vector2 direction = _hand.AimDirection;
        Vector2 end = origin + direction * _range;
        ContactFilter2D filter = TargetFilter();
        Physics2D.Raycast(origin, direction, filter, _hits, _range);
        _hits.Sort(static (a, b) => a.distance.CompareTo(b.distance));

        ActorHealth target = null;
        foreach (RaycastHit2D hit in _hits)
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.GetComponentInParent<ActorStats>() == OwnerStats)
                continue;
            target = GetEnemy(collider);
            if (target == null && collider.isTrigger)
                continue;
            end = hit.point;
            break;
        }

        _path.Clear();
        _path.Add(origin);
        _path.Add(end);
        _struck.Clear();
        int bounces = _baseBounces + Mathf.FloorToInt(OwnerStats.ProjectilePiercing.Value);
        for (int remaining = bounces; target != null; remaining--)
        {
            if (target == owner)
            {
                ApplySelfDamage(owner, _damage * 0.5f);
                break;
            }
            _struck.Add(target);
            Vector2 center = target.transform.position;
            HitEnemy(target, _damage);
            if (remaining == 0)
                break;
            target = FindBounce(center, owner, filter);
            if (target != null)
                _path.Add(target.transform.position);
        }

        NetworkEffects.Play(OwnerNetworkObject.NetworkManager, _chainEffectId, origin, path: BuildBolt(_path, 0.28f));
    }

    private ActorHealth FindBounce(Vector2 center, ActorHealth owner, ContactFilter2D filter)
    {
        float closest = _bounceRadius * _bounceRadius;
        ActorHealth next = null;
        if (!owner.IsDead)
        {
            float ownerDistance = ((Vector2)owner.transform.position - center).sqrMagnitude;
            if (ownerDistance <= closest)
            {
                closest = ownerDistance;
                next = owner;
            }
        }

        Physics2D.OverlapCircle(center, _bounceRadius, filter, _overlaps);
        foreach (Collider2D collider in _overlaps)
        {
            ActorHealth candidate = GetEnemy(collider);
            if (candidate == null || _struck.Contains(candidate))
                continue;
            float distance = ((Vector2)candidate.transform.position - center).sqrMagnitude;
            if (distance > closest || (distance == closest && next != null))
                continue;
            closest = distance;
            next = candidate;
        }
        return next;
    }

    private void StrikeStorm()
    {
        float cooldown = GetReducedCooldown(BaseCooldown);
        SetCooldown(cooldown, OwnerNetworkObject.NetworkManager.ServerTime.Time + cooldown);
        NotifyStateChanged();

        ActorHealth owner = OwnerStats.GetComponent<ActorHealth>();
        Vector2 center = owner.transform.position;
        Physics2D.OverlapCircle(center, _stormRadius, TargetFilter(), _overlaps);
        ApplySelfDamage(owner, _stormDamage);

        _struck.Clear();
        int hitCount = 0;
        foreach (Collider2D collider in _overlaps)
        {
            if (collider == null)
                continue;
            ActorHealth target = GetEnemy(collider);
            if (target != null && _struck.Add(target) && HitEnemy(target, _stormDamage))
                hitCount++;
        }
        owner.Heal(hitCount * _healPerEnemy);
        var ring = new Vector2[33];
        for (int i = 0; i < ring.Length; i++)
        {
            float angle = i * (2f * Mathf.PI / (ring.Length - 1));
            ring[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _stormRadius;
        }
        var path = new[] { center + Vector2.up * 7f, center };
        NetworkEffects.Play(OwnerNetworkObject.NetworkManager, _stormEffectId, center,
            path: BuildBolt(path, 0.6f), detail: ring);
    }

    private void ApplySelfDamage(ActorHealth owner, float damage)
    {
        const float minimumHealth = 0.1f;
        float allowedDamage = owner.CurrentHealth - minimumHealth;
        if (allowedDamage <= 0f)
            return;

        float potentialDamage = DamageCalculator.CalculateDamage(damage, EDamageType.Magic, OwnerStats, OwnerStats);
        if (potentialDamage > allowedDamage)
            damage *= allowedDamage / potentialDamage;

        owner.TakeDamage(damage, EDamageType.Magic, OwnerStats);
    }

    private bool HitEnemy(ActorHealth target, float damage)
    {
        Vector2 point = target.transform.position;
        if (DamageCalculator.DealCriticalDamage(target, damage, EDamageType.Magic, OwnerStats) <= 0f)
            return false;
        NotifyHit(target, EAttackType.Magic, point);
        return true;
    }

    private static Vector2[] BuildBolt(IReadOnlyList<Vector2> path, float maxBend)
    {
        const int segments = 5;
        var bolt = new Vector2[(path.Count - 1) * segments + 1];
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 start = path[i];
            Vector2 delta = path[i + 1] - start;
            Vector2 side = Vector2.Perpendicular(delta.normalized);
            float bend = Mathf.Min(delta.magnitude * 0.17f, maxBend);
            bolt[i * segments] = start;
            for (int j = 1; j < segments; j++)
            {
                float sign = (i + j) % 2 == 0 ? 1f : -1f;
                float strength = Mathf.Lerp(0.45f, 1f, Mathf.PerlinNoise(start.x + i, start.y + j));
                bolt[i * segments + j] = start + delta * ((float)j / segments) + side * (sign * bend * strength);
            }
        }
        bolt[^1] = path[path.Count - 1];
        return bolt;
    }

}
