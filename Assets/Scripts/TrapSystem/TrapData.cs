using UnityEngine;

public struct TrapData
{
    public ActorStats Owner;
    public Weapon SourceWeapon;
    public EffectData EffectOnCaught;
    public LayerMask TargetsLayerMask;
    public float Damage;
    public EDamageType DamageType;
    public float LifeTime;
    public float TriggerInterval;
    public float TriggerDelay;
    public float Radius;
    public int OperationsCount;
}
