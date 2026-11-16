using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Nyx/Grace")]
public sealed class NyxGraceConfig : SpellConfig
{
    [Min(0f)] public float Damage = 2f;
    [Min(0f)] public float HopSpeed = 11f;
    [Min(0.01f)] public float HopDuration = 0.24f;
    public EffectData Stun;
    public NyxGraceWave WavePrefab;

    public override Spell AddSpell(GameObject owner) => owner.AddComponent<NyxGraceSpell>();
    public override float GetRange(ActorStats owner) => WavePrefab.Radius * 2f;
}
