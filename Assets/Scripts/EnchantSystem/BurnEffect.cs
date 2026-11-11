using UnityEngine;

public sealed class BurnEffect : Effect
{
    private Weapon _weapon;
    private ActorStats _attacker;
    private ActorHealth _health;
    private float _damagePerSecond;
    private float _elapsed;
    private float _tickInterval;

    public void Configure(Weapon weapon, float damagePerSecond)
    {
        _weapon = weapon;
        _attacker = weapon.OwnerStats;
        _damagePerSecond = damagePerSecond;
    }

    public void Configure(ActorStats attacker)
    {
        BurnEffectData data = (BurnEffectData)EffectData;
        _attacker = attacker;
        _damagePerSecond = data.DamagePerSecond;
        _tickInterval = data.TickInterval;
        _elapsed = 0f;
    }

    public override void EffectStart(ActorStats owner)
    {
        _health = owner.GetComponent<ActorHealth>();
    }

    public override void EffectTick(ActorStats owner, float deltaTime)
    {
        if (_health.IsDead)
        {
            TimeRemaining = 0f;
            return;
        }
        _elapsed += deltaTime;
        float tickInterval = _tickInterval > 0f ? _tickInterval : 0.5f;
        if (_elapsed < tickInterval && TimeRemaining > 0f)
            return;

        float tickDuration = _elapsed;
        if (_tickInterval > 0f)
            tickDuration = _tickInterval;
        _elapsed -= tickDuration;
        _health.TakeDamage(_damagePerSecond * tickDuration, EDamageType.Magic, _attacker);
        if (_health.IsDead)
        {
            if (_weapon != null && _weapon.OwnerStats == _attacker)
                _weapon.NotifyKill(_health);
            TimeRemaining = 0f;
        }
    }
}
