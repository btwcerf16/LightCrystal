using UnityEngine;

public struct ProjectileData
{
    public Vector2 Direction;
    public float Speed;
    public float Damage;
    public float LifeTime;
    public LayerMask LayerMask;
    public EDamageType DamageType;
    public ActorStats Attacker;
    public Weapon SourceWeapon;
    public EffectData EffectOnHit;
    public bool IgnoreItemEffects;
    public bool StatsApplied;
    public bool IsArrow;
}
