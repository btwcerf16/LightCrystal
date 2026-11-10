using UnityEngine;
[CreateAssetMenu(menuName = "Spells/Enemies/Spider Web")]
public sealed class SpiderWebSpellConfig : SpellConfig
{
    public SpiderWebProjectile Projectile;
    public SpiderWebEffectData WebEffect;
    public LayerMask TargetMask;
    public override float GetRange(ActorStats owner) => owner.ProjectileRange.Value;
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<SpiderWebSpell>();
}
