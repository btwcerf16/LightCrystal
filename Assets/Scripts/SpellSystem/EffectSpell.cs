public sealed class EffectSpell : Spell
{
    protected override void Cast(SpellCastData data)
    {
        GetComponent<EffectHandler>().AddEffect(((EffectSpellConfig)SpellData).Effect);
    }
}
