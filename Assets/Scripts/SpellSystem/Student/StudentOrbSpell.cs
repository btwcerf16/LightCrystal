using Unity.Netcode;
using UnityEngine;

public sealed class StudentOrbSpell : Spell
{
    public float Range => ((StudentOrbConfig)SpellData).GetRange(_ownerStats);
    public float Damage => DamageCalculator.CalculateDamage(((StudentOrbConfig)SpellData).Damage, EDamageType.Magic, _ownerStats, null);

    public override bool CanCast(SpellCastData data) => ((StudentOrbConfig)SpellData).Projectile != null;

    protected override void Cast(SpellCastData data)
    {
        StudentOrbConfig config = (StudentOrbConfig)SpellData;
        ActorHand hand = GetComponentInChildren<ActorHand>();
        NetworkManager manager = GetComponent<NetworkObject>().NetworkManager;
        MagicOrbProjectile orb = manager.GetComponent<NetworkObjectPool>().GetObject(config.Projectile, hand.Position, Quaternion.identity);
        orb.Launch(new ProjectileData
        {
            Attacker = _ownerStats,
            DamageType = EDamageType.Magic,
            Damage = config.Damage,
            Direction = hand.AimDirection,
            Speed = config.Speed,
            LifeTime = config.Range / config.Speed,
            LayerMask = config.TargetMask
        });
        orb.GetComponent<NetworkObject>().Spawn();
    }
}
