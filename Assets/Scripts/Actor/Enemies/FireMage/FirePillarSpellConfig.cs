using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Enemies/Fire Pillar")]
public sealed class FirePillarSpellConfig : SpellConfig
{
    public DamageTrap Pillar;
    public BurnEffectData Burning;
    public LayerMask TargetMask;
    [Min(0f)] public float Damage = 4f;
    [Min(0.01f)] public float Radius = 1.5f;
    [Min(0.01f)] public float TriggerDelay = 1f;

    public override Spell AddSpell(GameObject owner) => owner.AddComponent<FirePillarSpell>();
}
