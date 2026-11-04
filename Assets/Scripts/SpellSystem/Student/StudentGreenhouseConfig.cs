using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Student/Greenhouse Mage")]
public sealed class StudentGreenhouseConfig : SpellConfig
{
    public float Radius = 10f;
    public float DamageBonus = 0.2f;
    public float DamagePenalty = -0.1f;
    public override float GetRange(ActorStats owner) => Radius;
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<StudentGreenhouseSpell>();
}
