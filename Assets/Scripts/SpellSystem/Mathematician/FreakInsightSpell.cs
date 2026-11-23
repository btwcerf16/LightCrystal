public sealed class FreakInsightSpell : Spell
{
    private EmpiricalLearningSpell _learning;
    private EffectHandler _effects;
    private Effect _insight;

    public override void Activate()
    {
        base.Activate();
        _learning = GetComponent<EmpiricalLearningSpell>();
        _effects = GetComponent<EffectHandler>();
    }

    public override bool CanCast(SpellCastData data) => _learning != null && _learning.Stacks >= EmpiricalLearningSpell.MaxStacks;

    protected override void Cast(SpellCastData data)
    {
        if (_learning.SpendStacks())
            _insight = _effects.AddEffect(((FreakInsightConfig)SpellData).Effect);
    }

    public override void Deactivate()
    {
        if (_insight != null)
            _effects.RemoveEffect(_insight);
        base.Deactivate();
    }
}
