using UnityEngine;

public sealed class ToggleSpell : Spell
{
    private Effect _effect;
    public override float SanityCost => base.SanityCost;
    public override bool IsToggled => _effect != null && GetComponent<EffectHandler>().HasEffect(_effect.EffectData);
    public override bool CanCast(SpellCastData data) => IsToggled || GetComponent<ActorSanity>().CurrentSanity > 0f;

    protected override void Cast(SpellCastData data)
    {
        if (IsToggled)
            StopEffect();
        else
            _effect = GetComponent<EffectHandler>().AddEffect(((ToggleSpellConfig)SpellData).Effect);

    }

    private void Update()
    {
        if (!IsToggled)
            return;
        EffectHandler effects = GetComponent<EffectHandler>();
        float cost = ((ToggleSpellConfig)SpellData).SanityPerSecond * (1f - _ownerStats.SanityCostReduction.Value) * Time.deltaTime;
        if (GetComponent<ActorHealth>().IsDead || effects.IsStunned || effects.IsSilenced ||
            !GetComponent<ActorSanity>().TrySpendSanity(cost))
            StopEffect();
    }

    private void StopEffect()
    {
        if (_effect != null)
            GetComponent<EffectHandler>().RemoveEffect(_effect);
        _effect = null;
    }

    public override void Deactivate()
    {
        StopEffect();
        base.Deactivate();
    }
}
