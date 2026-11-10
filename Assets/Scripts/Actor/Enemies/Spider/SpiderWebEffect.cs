public sealed class SpiderWebEffect : Effect
{
    public override void EffectStart(ActorStats owner)
    {
        float slow = -((SpiderWebEffectData)EffectData).Slow;
        owner.MoveSpeed.AddModifier(new StatModifier(slow, StatModifierType.AdditivePercent, this));
        owner.AttackSpeed.AddModifier(new StatModifier(slow, StatModifierType.AdditivePercent, this));
    }
    public override void EffectEnd(ActorStats owner)
    {
        owner.MoveSpeed.RemoveModifiersFrom(this);
        owner.AttackSpeed.RemoveModifiersFrom(this);
    }
}
