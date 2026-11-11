using Unity.Netcode;
using UnityEngine;

public sealed class FirePillarSpell : Spell
{
    public override bool CanCast(SpellCastData data) => data.Target != null;

    protected override void Cast(SpellCastData data)
    {
        FirePillarSpellConfig config = (FirePillarSpellConfig)SpellData;
        NetworkManager manager = GetComponent<NetworkObject>().NetworkManager;
        Vector2 center = data.Target.transform.position;
        DamageTrap pillar = manager.GetComponent<NetworkObjectPool>().GetObject(config.Pillar, center, Quaternion.identity);
        pillar.Initialize(new TrapData
        {
            Owner = OwnerStats,
            EffectOnCaught = config.Burning,
            TargetsLayerMask = config.TargetMask,
            Damage = config.Damage,
            DamageType = EDamageType.Magic,
            TriggerDelay = config.TriggerDelay,
            Radius = config.Radius
        });
        pillar.GetComponent<NetworkObject>().Spawn();
    }
}
