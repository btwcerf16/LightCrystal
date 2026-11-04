using UnityEngine;

public sealed class CoinCurse : Effect
{
    private float _elapsed;

    public override void EffectStart(ActorStats owner)
    {
        CoinCurseData data = (CoinCurseData)EffectData;
        if (data.Curse == ECoinCurse.Blindness)
            owner.ViewDistance.AddModifier(new StatModifier(0f, StatModifierType.Multiplier, this));
        if (data.Curse == ECoinCurse.Defenseless)
        {
            owner.PhysicalResistance.AddModifier(new StatModifier(0f, StatModifierType.Multiplier, this));
            owner.MagicResistance.AddModifier(new StatModifier(0f, StatModifierType.Multiplier, this));
            owner.MoveSpeed.AddModifier(new StatModifier(1f - data.MoveSpeedPenalty, StatModifierType.Multiplier, this));
        }
    }

    public override void EffectTick(ActorStats owner, float deltaTime)
    {
        ActorHealth health = owner.GetComponent<ActorHealth>();
        if (health.IsDead) { TimeRemaining = 0f; return; }
        _elapsed += deltaTime;
        if (_elapsed < 1f && TimeRemaining > 0f)
            return;
        float elapsed = _elapsed;
        _elapsed = 0f;
        CoinCurseData data = (CoinCurseData)EffectData;
        if (data.Curse == ECoinCurse.Blindness)
            health.TakeDamage(health.MaxHealth * data.HealthPerSecond * elapsed, EDamageType.Magic);
        if (data.Curse == ECoinCurse.SanityDrain)
        {
            ActorSanity sanity = owner.GetComponent<ActorSanity>();
            sanity.DrainSanity((data.SanityPerSecond + (sanity.MaxSanity - sanity.CurrentSanity) * data.MissingSanityPerSecond) * elapsed);
        }
    }

    public override void EffectEnd(ActorStats owner)
    {
        owner.ViewDistance.RemoveModifiersFrom(this);
        owner.PhysicalResistance.RemoveModifiersFrom(this);
        owner.MagicResistance.RemoveModifiersFrom(this);
        owner.MoveSpeed.RemoveModifiersFrom(this);
    }
}
