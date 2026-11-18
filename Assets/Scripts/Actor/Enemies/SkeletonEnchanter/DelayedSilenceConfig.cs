using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Enemies/Delayed Silence")]
public sealed class DelayedSilenceConfig : SpellConfig
{
    [Min(0.01f)] public float Delay = 2f;
    [Min(0.01f)] public float SilenceDuration = 2f;
    public SilenceMarkData Mark;
    public StatusEffectData Silence;

    public override Spell AddSpell(GameObject owner) => owner.AddComponent<DelayedSilenceSpell>();
}
