public sealed class StatisticalErrorEffect : Effect
{
    private ActorHealth _health;
    private EffectHandler _effects;

    public override void EffectStart(ActorStats owner)
    {
        _health = owner.GetComponent<ActorHealth>();
        _effects = owner.GetComponent<EffectHandler>();
        _health.AbsorbNextHit = AbsorbHit;
        _health.Died += RemoveEffect;
    }

    private void AbsorbHit()
    {
        GetComponent<EmpiricalLearningSpell>()?.AddStacks(3f);
        RemoveEffect();
    }

    private void RemoveEffect() => _effects.RemoveEffect(this);

    public override void EffectEnd(ActorStats owner)
    {
        if (_health.AbsorbNextHit == AbsorbHit)
            _health.AbsorbNextHit = null;
        _health.Died -= RemoveEffect;
    }
}
