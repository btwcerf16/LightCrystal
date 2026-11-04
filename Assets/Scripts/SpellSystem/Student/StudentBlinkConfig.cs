using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Student/Blink")]
public sealed class StudentBlinkConfig : SpellConfig
{
    [Min(0f)] public float Range = 10f;
    public EffectData Stun;
    public override float GetRange(ActorStats owner) => Range;
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<StudentBlinkSpell>();
}
