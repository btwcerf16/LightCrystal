using UnityEngine;

public sealed class SanityShield : Effect
{
    private ActorSanity _sanity;
    private ActorStats _stats;
    private ActorHealth _health;
    private EffectHandler _handler;
    private bool _active;

    public override void EffectStart(ActorStats owner)
    {
        _stats = owner;
        _sanity = owner.GetComponent<ActorSanity>();
        _handler = owner.GetComponent<EffectHandler>();
        _health = owner.GetComponent<ActorHealth>();
        _active = _sanity != null && _health != null;
        if (_active)
        {
            _health.Shield = this;
            _sanity.SanityDepleted += Break;
        }
    }

    public float AbsorbDamage(float damage)
    {
        if (!_active || damage <= 0f)
            return damage;
        if (_sanity.CurrentSanity <= 0f)
        {
            Break();
            return damage;
        }
        float costPerDamage = 1f - Mathf.Clamp01(_stats.MadnessResistance.Value);
        if (costPerDamage <= 0f)
            return 0f;
        float absorbed = Mathf.Min(damage, _sanity.CurrentSanity / costPerDamage);
        if (damage * costPerDamage >= _sanity.CurrentSanity)
            _sanity.EmptySanity();
        else
            _sanity.DrainSanity(damage);
        return Mathf.Max(0f, damage - absorbed);
    }

    private void Break()
    {
        if (!_active)
            return;
        EffectData stun = ((SanityShieldData)EffectData).BreakStun;
        _handler.RemoveEffect(this);
        if (stun != null)
            _handler.AddEffect(stun);
    }

    public override void EffectEnd(ActorStats owner)
    {
        _active = false;
        if (_health != null && _health.Shield == this)
            _health.Shield = null;
        if (_sanity != null)
            _sanity.SanityDepleted -= Break;
    }
}
