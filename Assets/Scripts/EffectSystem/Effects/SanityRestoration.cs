public sealed class SanityRestoration : Effect
{
    private ActorSanity _sanity;
    private float _perSecond;

    public override void EffectStart(ActorStats owner)
    {
        _sanity = owner.GetComponent<ActorSanity>();
        SanityRestorationData data = (SanityRestorationData)EffectData;
        _perSecond = owner.MaxSanity.Value * data.RestorePercent / data.EffectDuration;
    }

    public override void EffectTick(ActorStats owner, float deltaTime)
    {
        ActorHealth health = owner.GetComponent<ActorHealth>();
        if (_sanity != null && (health == null || !health.IsDead))
            _sanity.RestoreSanity(_perSecond * deltaTime);
    }
}
