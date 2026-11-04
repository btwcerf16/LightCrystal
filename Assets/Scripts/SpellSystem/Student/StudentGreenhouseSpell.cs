using System.Collections.Generic;
using UnityEngine;

public sealed class StudentGreenhouseSpell : PassiveSpell
{
    private readonly HashSet<Enemy> _enemies = new();
    private float _nextCheck;
    private float _bonus = float.NaN;

    protected override void Apply() => Refresh();

    private void Update()
    {
        if (Time.time < _nextCheck)
            return;
        _nextCheck = Time.time + 0.25f;
        Refresh();
    }

    private void Refresh()
    {
        StudentGreenhouseConfig config = (StudentGreenhouseConfig)SpellData;
        _enemies.Clear();
        foreach (Collider2D target in Physics2D.OverlapCircleAll(transform.position, config.Radius))
        {
            Enemy enemy = target.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.GetComponent<ActorHealth>().IsDead)
                _enemies.Add(enemy);
        }
        int limit = Mathf.Max(1, Mathf.CeilToInt(GetComponent<ActorSanity>().CurrentSanity / 100f));
        float bonus = _enemies.Count <= limit ? config.DamageBonus : config.DamagePenalty;
        if (_bonus == bonus)
            return;
        _bonus = bonus;
        _ownerStats.MagicDamageMultiplier.RemoveModifiersFrom(this);
        _ownerStats.MagicDamageMultiplier.AddModifier(new StatModifier(bonus, StatModifierType.Flat, this));
    }

    protected override void Remove()
    {
        _ownerStats.MagicDamageMultiplier.RemoveModifiersFrom(this);
        _bonus = float.NaN;
    }
}
