using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Nyx/Plan B")]
public sealed class NyxPlanBConfig : SpellConfig
{
    [Min(0f)] public float BlinkDistance = 4f;
    [Min(0f)] public float AutoCooldown = 30f;
    public EffectData Stun;
    public NyxGraceWave WavePrefab;

    public override Spell AddSpell(GameObject owner) => owner.AddComponent<NyxPlanBSpell>();
    public override float GetRange(ActorStats owner) => WavePrefab.Radius;
}
