public sealed class FirePillarTrap : DamageTrap
{
    protected override void OnEffectApplied(Effect effect)
    {
        if (effect is BurnEffect burn)
            burn.Configure(_owner);
    }
}
