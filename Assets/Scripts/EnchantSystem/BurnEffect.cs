using UnityEngine;

public sealed class BurnEffect : Effect
{
    private Weapon _weapon;
    private ActorStats _attacker;
    private ActorHealth _health;
    private float _damagePerSecond;
    private float _elapsed;

    public void Configure(Weapon weapon, float damagePerSecond)
    {
        _weapon = weapon;
        _attacker = weapon.OwnerStats;
        _damagePerSecond = damagePerSecond;
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
        if (_elapsed < 0.5f && TimeRemaining > 0f)
            return;
        _health.TakeDamage(_damagePerSecond * _elapsed, EDamageType.Magic, _attacker);
        _elapsed = 0f;
        if (_health.IsDead)
        {
            if (_weapon != null && _weapon.OwnerStats == _attacker)
                _weapon.NotifyKill(_health);
            TimeRemaining = 0f;
        }
    }
}
