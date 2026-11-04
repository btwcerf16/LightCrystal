public sealed class KnightHealSpell : Spell
{
    protected override void Cast(SpellCastData data)
    {
        EffectHandler effects = GetComponent<EffectHandler>();
        int removed = 0;
        for (int i = effects.ActiveEffects.Count - 1; i >= 0; i--)
        {
            Effect effect = effects.ActiveEffects[i];
            if (effect.EffectData.IsBuff || !effect.EffectData.CanPurged)
                continue;
            effects.RemoveEffect(effect);
            removed++;
        }
        GetComponent<ActorSanity>().RestoreSanity(removed * ((KnightHealConfig)SpellData).SanityPerDebuff);
    }
}
