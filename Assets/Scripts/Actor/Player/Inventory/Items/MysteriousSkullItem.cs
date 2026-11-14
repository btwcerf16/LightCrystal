using UnityEngine;

public sealed class MysteriousSkullItem : Item
{
    [SerializeField] private float _damagePerKill = 0.1f;
    [SerializeField] private float _healthPenalty = 0.2f;
    private ActorHealth _health;
    private int _kills;
    public override string DisplayName => "Загадочный череп";
    public float DamagePerKill => _damagePerKill;
    public float HealthPenaltyPercent => _healthPenalty * 100f;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _health = _ownerStats.GetComponent<ActorHealth>();
        _health.EnemyKilled += OnEnemyKilled;
        _ownerStats.MaxHealth.AddModifier(new StatModifier(1f - _healthPenalty, StatModifierType.Multiplier, this));
        _ownerStats.PhysicalDamage.AddModifier(new StatModifier(_kills * _damagePerKill, StatModifierType.Flat, this));
    }

    private void OnEnemyKilled(ActorHealth enemy)
    {
        _kills++;
        _ownerStats.PhysicalDamage.RemoveModifiersFrom(this);
        _ownerStats.PhysicalDamage.AddModifier(new StatModifier(_kills * _damagePerKill, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (_health != null)
            _health.EnemyKilled -= OnEnemyKilled;
        if (IsServer)
        {
            _ownerStats.MaxHealth.RemoveModifiersFrom(this);
            _ownerStats.PhysicalDamage.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    public override void Use() { }
}
