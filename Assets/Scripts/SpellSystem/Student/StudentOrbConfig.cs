using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Student/Magic Orb")]
public sealed class StudentOrbConfig : SpellConfig
{
    public MagicOrbProjectile Projectile;
    public float Damage = 3f;
    public float Range = 10f;
    public float Speed = 12f;
    public LayerMask TargetMask = ~0;
    public override float GetRange(ActorStats owner) => Mathf.Max(0f, Range + (owner != null ? owner.ProjectileRange.Value : 0f));
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<StudentOrbSpell>();
}
