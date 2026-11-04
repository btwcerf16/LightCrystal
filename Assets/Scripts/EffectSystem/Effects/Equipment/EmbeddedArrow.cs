public sealed class EmbeddedArrow : Effect
{
    private float _damagePerSecond;
    private float _elapsed;
    private ActorStats _attacker;
    private Weapon _weapon;
    private ActorHealth _health;
    public AncientQuiverItem Source { get; private set; }
    public float RemainingDamage => _damagePerSecond * (TimeRemaining + _elapsed);

    public void Configure(AncientQuiverItem source, ActorStats attacker, Weapon weapon, float damagePerSecond)
    {
        Source = source;
        _attacker = attacker;
        _weapon = weapon;
        _damagePerSecond = damagePerSecond;
    }

    public override void EffectStart(ActorStats owner) => _health = owner.GetComponent<ActorHealth>();

    public override void EffectTick(ActorStats owner, float deltaTime)
    {
        if (_health.IsDead) { TimeRemaining = 0f; return; }
        _elapsed += deltaTime;
        while (_elapsed >= 1f && !_health.IsDead)
        {
            _elapsed -= 1f;
            _health.TakeDamage(_damagePerSecond, EDamageType.Pure, _attacker);
        }
        if (TimeRemaining <= 0f && _elapsed > 0f && !_health.IsDead)
        {
            _health.TakeDamage(_damagePerSecond * _elapsed, EDamageType.Pure, _attacker);
            _elapsed = 0f;
        }
    }

    public void Pull()
    {
        if (_health.IsDead) return;
        float damage = RemainingDamage;
        GetComponent<EffectHandler>().RemoveEffect(this);
        _health.TakeDamage(damage, EDamageType.Pure, _attacker);
        if (_weapon != null && _weapon.OwnerStats == _attacker)
            _weapon.NotifyHit(_health);
    }
}
