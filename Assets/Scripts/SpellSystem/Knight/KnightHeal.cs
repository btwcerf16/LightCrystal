using UnityEngine;

public sealed class KnightHeal : Spell
{
    private ActorHealth _health;

    private void Awake()
    {
        _health = GetComponent<ActorHealth>();
    }

    public override bool CanCast(SpellCastData data)
    {
        return !_health.IsDead && _health.CurrentHealth < _health.MaxHealth;
    }

    protected override void Cast(SpellCastData data)
    {
        _health.Heal(((KnightHealConfig)SpellData).Healing);
    }
}
