public sealed class SilenceMarkEffect : Effect
{
    private EffectData _silence;
    private float _duration;

    public void SetSilence(EffectData silence, float duration)
    {
        _silence = silence;
        _duration = duration;
    }

    public override void EffectEnd(ActorStats owner)
    {
        if (TimeRemaining <= 0f && !owner.GetComponent<ActorHealth>().IsDead)
            owner.GetComponent<EffectHandler>().AddEffect(_silence, _duration);
    }
}
