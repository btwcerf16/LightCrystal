using UnityEngine;

public sealed class ToggleEffectSpell : ToggleSpell
{
    private Effect _effect;
    private ToggleSpellConfig EffectConfig => (ToggleSpellConfig)SpellData;
    public override bool IsToggled => base.IsToggled && _effect != null && _ownerEffects.HasEffect(_effect.EffectData);
    public override bool CanCast(SpellCastData data) => IsToggled || (EffectConfig.Effect != null &&
        (EffectConfig.SanityPerSecond == 0f || _ownerSanity.CurrentSanity > 0f));

    protected override void OnToggleChanged(bool isToggled)
    {
        if (isToggled)
        {
            _effect = _ownerEffects.AddEffect(EffectConfig.Effect);
            return;
        }

        if (_effect != null)
            _ownerEffects.RemoveEffect(_effect);
        _effect = null;
    }

    private void Update()
    {
        if (!base.IsToggled)
            return;

        float sanityCost = GetReducedSanityCost(EffectConfig.SanityPerSecond) * Time.deltaTime;
        if (!IsToggled || GetComponent<ActorHealth>().IsDead || _ownerEffects.IsStunned || _ownerEffects.IsSilenced ||
            (sanityCost > 0f && !_ownerSanity.TrySpendSanity(sanityCost)))
            StopToggle();
    }
}
