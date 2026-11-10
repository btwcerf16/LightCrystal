using Unity.Netcode;
using UnityEngine;
public sealed class SpiderWebSpell : Spell
{
    public override bool CanCast(SpellCastData data)
    {
        if (data.Target == null || data.Target.GetComponent<PlayerInputReader>() == null)
            return false;
        return !data.Target.GetComponent<ActorHealth>().IsDead && ((Vector2)(data.Target.transform.position - transform.position)).sqrMagnitude <= _ownerStats.ProjectileRange.Value * _ownerStats.ProjectileRange.Value;
    }
    protected override void Cast(SpellCastData data)
    {
        SpiderWebSpellConfig config = (SpiderWebSpellConfig)SpellData;
        Vector2 direction = data.Target.transform.position - transform.position;
        NetworkManager manager = GetComponent<NetworkObject>().NetworkManager;
        SpiderWebProjectile web = manager.GetComponent<NetworkObjectPool>().GetObject(config.Projectile, transform.position + (Vector3)direction.normalized * 0.4f, Quaternion.identity);
        web.Launch(new ProjectileData { Attacker = _ownerStats, Direction = direction, LayerMask = config.TargetMask, EffectOnHit = config.WebEffect, DamageType = EDamageType.Physical, IgnoreItemEffects = true });
        web.GetComponent<NetworkObject>().Spawn();
    }
}
