using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Toggle Effect")]
public sealed class ToggleSpellConfig : SpellConfig
{
    public EffectData Effect;
    [Min(0f)] public float SanityPerSecond;
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<ToggleSpell>();
}
