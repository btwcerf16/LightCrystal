using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class ChurchBlacklistItem : Item
{
    [SerializeField] private float _magicDamageBonus = 0.25f;
    [SerializeField] private float _viewBonus = 0.2f;
    [SerializeField] private int _extraPiercing = 2;
    [SerializeField] private float _extraProjectileDamage = 0.35f;
    [SerializeField] private float _brandDamage = 0.25f;
    [SerializeField] private float _healingPerEnemy = 0.01f;
    [SerializeField] private float _maximumHealing = 0.15f;
    [SerializeField] private float _activeCooldown = 30f;
    [SerializeField] private ChurchBrandData _brand;
    [SerializeField] private ChurchFireProjectile _firePrefab;
    private readonly List<ChurchBrand> _brands = new();
    public float MagicDamageBonus => _magicDamageBonus;
    public float ViewBonus => _viewBonus;
    public int ExtraPiercing => _extraPiercing;
    public float ExtraProjectileDamage => _extraProjectileDamage;
    public float BrandDamage => _brandDamage;
    public float HealingPerEnemy => _healingPerEnemy;
    public float MaximumHealing => _maximumHealing;
    public override string DisplayName => "Чёрный список сожжённой церкви";
    public override float BaseCooldown => _activeCooldown;

    public override void Equip()
    {
        if (IsEquipped) return;
        base.Equip();
        if (!IsServer) return;
        _ownerStats.MagicDamageMultiplier.AddModifier(new StatModifier(_magicDamageBonus, StatModifierType.Flat, this));
        _ownerStats.ViewDistance.AddModifier(new StatModifier(_viewBonus, StatModifierType.AdditivePercent, this));
        _ownerStats.ProjectilePiercing.AddModifier(new StatModifier(_extraPiercing, StatModifierType.Flat, this));
        Projectile.Launched += OnLaunch;
    }

    private void OnLaunch(Projectile projectile)
    {
        ProjectileData shot = projectile.ShotData;
        if (shot.Attacker != _ownerStats || shot.IgnoreItemEffects || CooldownRemaining > 0f || _firePrefab == null) return;
        NetworkObjectPool pool = _ownerStats.GetComponent<NetworkObject>().NetworkManager.GetComponent<NetworkObjectPool>();
        float[] angles = { -24f, -8f, 8f, 24f };
        foreach (float angle in angles)
        {
            ChurchFireProjectile fire = pool.GetObject(_firePrefab, projectile.transform.position, Quaternion.identity);
            ProjectileData data = shot;
            data.IgnoreItemEffects = true;
            data.IsArrow = false;
            data.StatsApplied = true;
            data.Direction = Quaternion.Euler(0f, 0f, angle) * shot.Direction;
            data.Damage *= _extraProjectileDamage;
            fire.SetSource(this);
            fire.Launch(data);
            fire.GetComponent<NetworkObject>().Spawn();
        }
    }

    public void ApplyBrand(ActorHealth target, ProjectileData data)
    {
        if (!IsEquipped || CooldownRemaining > 0f || data.Attacker != _ownerStats) return;
        _brands.RemoveAll(brand => brand == null);
        foreach (ChurchBrand brand in _brands)
            if (brand.GetComponent<ActorHealth>() == target) return;
        ChurchBrand effect = target.GetComponent<EffectHandler>().AddEffect(_brand) as ChurchBrand;
        if (effect == null) return;
        // Fire deals a fraction of the shot; the brand scales from the original shot.
        float damage = _extraProjectileDamage > 0f ? data.Damage / _extraProjectileDamage * _brandDamage : 0f;
        effect.Configure(this, _ownerStats, damage, data.DamageType);
        _brands.Add(effect);
    }

    protected override bool CanUse()
    {
        return _brands.Exists(brand => brand != null && !brand.GetComponent<ActorHealth>().IsDead);
    }

    public override void Use()
    {
        if (!IsServer) return;
        int count = 0;
        foreach (ChurchBrand brand in _brands)
        {
            if (brand == null) continue;
            if (!brand.GetComponent<ActorHealth>().IsDead) count++;
            brand.GetComponent<EffectHandler>().RemoveEffect(brand);
        }
        _brands.Clear();
        ActorHealth health = _ownerStats.GetComponent<ActorHealth>();
        health.Heal(health.MaxHealth * Mathf.Min(_maximumHealing, count * _healingPerEnemy));
    }

    public override void Drop()
    {
        Projectile.Launched -= OnLaunch;
        if (IsServer)
        {
            _ownerStats.MagicDamageMultiplier.RemoveModifiersFrom(this);
            _ownerStats.ViewDistance.RemoveModifiersFrom(this);
            _ownerStats.ProjectilePiercing.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    private void OnDestroy() => Projectile.Launched -= OnLaunch;
}
