using System.Collections.Generic;
using UnityEngine;

public sealed class AncientQuiverItem : Item
{
    [SerializeField] private float _viewBonus = 0.15f;
    [SerializeField] private float _attackSpeedPenalty = 0.15f;
    [SerializeField] private float _arrowDamagePerSecond = 0.05f;
    [SerializeField] private int _maximumArrows = 6;
    [SerializeField] private float _pullRange = 2f;
    [SerializeField] private float _splashRadius = 3f;
    [SerializeField] private float _activeCooldown = 20f;
    [SerializeField] private EmbeddedArrowData _embeddedArrow;
    [SerializeField] private EffectData _bloodSlow;
    private readonly List<EmbeddedArrow> _arrows = new();
    private ActorHealth _target;
    public float ViewBonus => _viewBonus;
    public float AttackSpeedPenalty => _attackSpeedPenalty;
    public float ArrowDamagePerSecond => _arrowDamagePerSecond;
    public int MaximumArrows => _maximumArrows;
    public float PullRange => _pullRange;
    public float ArrowDuration => _embeddedArrow != null ? _embeddedArrow.EffectDuration : 0f;
    public override string DisplayName => "Древний колчан";
    public override float BaseCooldown => _activeCooldown;

    public override void Equip()
    {
        if (IsEquipped) return;
        base.Equip();
        if (!IsServer) return;
        _ownerStats.ViewDistance.AddModifier(new StatModifier(_viewBonus, StatModifierType.AdditivePercent, this));
        _ownerStats.AttackSpeed.AddModifier(new StatModifier(1f - _attackSpeedPenalty, StatModifierType.Multiplier, this));
        Projectile.Launched += OnLaunch;
        Projectile.TargetHit += OnHit;
    }

    private void OnLaunch(Projectile projectile)
    {
        if (projectile.ShotData.Attacker == _ownerStats && projectile.ShotData.IsArrow && !projectile.ShotData.IgnoreItemEffects)
            projectile.SetPiercing(0);
    }

    private void OnHit(Projectile projectile, ActorHealth target, float damage)
    {
        if (projectile.ShotData.Attacker != _ownerStats || !projectile.ShotData.IsArrow ||
            target.IsDead || target.GetComponent<Enemy>() == null || damage <= 0f || _embeddedArrow == null) return;
        int count = 0;
        EffectHandler effects = target.GetComponent<EffectHandler>();
        foreach (Effect effect in effects.ActiveEffects)
            if (effect is EmbeddedArrow arrow && arrow.Source == this) count++;
        if (count >= _maximumArrows) return;
        EmbeddedArrow embedded = effects.AddEffect(_embeddedArrow) as EmbeddedArrow;
        if (embedded != null)
        {
            embedded.Configure(this, _ownerStats, _ownerStats.GetComponent<ActorArsenal>().ActiveWeapon, damage * _arrowDamagePerSecond);
            _arrows.Add(embedded);
        }
    }

    protected override bool CanUse()
    {
        _arrows.RemoveAll(arrow => arrow == null);
        _target = null;
        float nearest = _pullRange * _pullRange;
        foreach (EmbeddedArrow arrow in _arrows)
        {
            ActorHealth health = arrow.GetComponent<ActorHealth>();
            float distance = (health.transform.position - _ownerStats.transform.position).sqrMagnitude;
            if (!health.IsDead && distance <= nearest) { _target = health; nearest = distance; }
        }
        return _target != null;
    }

    public override void Use()
    {
        if (!IsServer || _target == null) return;
        foreach (EmbeddedArrow arrow in _arrows.ToArray())
            if (arrow != null && arrow.GetComponent<ActorHealth>() == _target && !_target.IsDead)
            {
                arrow.Pull();
                _arrows.Remove(arrow);
            }
        if (!_target.IsDead) return;
        var affected = new HashSet<Enemy>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(_target.transform.position, _splashRadius))
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null && affected.Add(enemy) && !enemy.GetComponent<ActorHealth>().IsDead)
                enemy.GetComponent<EffectHandler>().AddEffect(_bloodSlow);
        }
    }

    public override void Drop()
    {
        Projectile.Launched -= OnLaunch;
        Projectile.TargetHit -= OnHit;
        if (IsServer)
        {
            _ownerStats.ViewDistance.RemoveModifiersFrom(this);
            _ownerStats.AttackSpeed.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    private void OnDestroy()
    {
        Projectile.Launched -= OnLaunch;
        Projectile.TargetHit -= OnHit;
    }
}
