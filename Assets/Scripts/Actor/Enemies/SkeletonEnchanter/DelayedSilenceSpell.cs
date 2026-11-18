using UnityEngine;

public sealed class DelayedSilenceSpell : Spell
{
    public override bool CanCast(SpellCastData data) => data.Target != null
        && data.Target.TryGetComponent<ActorHealth>(out var health) && !health.IsDead
        && data.Target.TryGetComponent<PlayerInputReader>(out _);

    protected override void Cast(SpellCastData data)
    {
        if (!CanCast(data) || !data.Target.TryGetComponent<EffectHandler>(out var effects))
            return;

        var config = (DelayedSilenceConfig)SpellData;
        if (effects.AddEffect(config.Mark, config.Delay) is SilenceMarkEffect mark)
            mark.SetSilence(config.Silence, config.SilenceDuration);
    }
}
