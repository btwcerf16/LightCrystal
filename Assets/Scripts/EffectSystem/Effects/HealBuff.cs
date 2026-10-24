public class HealBuff : Effect
{
    private ActorHealth _health;

    public override void EffectStart(ActorStats owner)
    {
        _health = owner.GetComponent<ActorHealth>();
    }

    public override void EffectTick(ActorStats owner, float deltaTime)
    {
        if (_health != null)
            _health.Heal(((HealBuffData)EffectData).HealCount * deltaTime);
    }
}
