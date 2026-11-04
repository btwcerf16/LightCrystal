public sealed class ChurchBrand : Effect
{
    private Enemy _enemy;
    private ActorHealth _health;
    private ActorStats _attacker;
    private float _damage;
    private EDamageType _damageType;
    public ChurchBlacklistItem Source { get; private set; }

    public void Configure(ChurchBlacklistItem source, ActorStats attacker, float damage, EDamageType damageType)
    {
        Source = source;
        _attacker = attacker;
        _damage = damage;
        _damageType = damageType;
    }

    public override void EffectStart(ActorStats owner)
    {
        _enemy = owner.GetComponent<Enemy>();
        _health = owner.GetComponent<ActorHealth>();
        _enemy.AttackStarted += Burn;
    }

    private void Burn()
    {
        if (!_health.IsDead) _health.TakeDamage(_damage, _damageType, _attacker);
    }

    public override void EffectTick(ActorStats owner, float deltaTime)
    {
        if (_health.IsDead) TimeRemaining = 0f;
    }

    public override void EffectEnd(ActorStats owner) => _enemy.AttackStarted -= Burn;
}
