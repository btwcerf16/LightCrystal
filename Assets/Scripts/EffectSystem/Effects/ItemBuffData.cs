using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Buffs/Item Buff", fileName = "ItemBuff")]
public sealed class ItemBuffData : EffectData
{
    public float AttackSpeed;
    public float MoveSpeed;
    public float CriticalDamage;
    public float CriticalChance;
    public float CameraDistance;
    public float MadnessResistance;
    public bool FreeSpellCooldowns;
    public float CriticalChancePercent => CriticalChance * 100f;
    public float CameraDistancePercent => CameraDistance * 100f;
    public float MadnessResistancePercent => MadnessResistance * 100f;

    public override Effect CreateEffect(GameObject owner)
    {
        ItemBuff effect = owner.AddComponent<ItemBuff>();
        effect.Initialize(this);
        return effect;
    }
}
