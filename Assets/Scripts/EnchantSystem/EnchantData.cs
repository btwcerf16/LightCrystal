using UnityEngine;

public abstract class EnchantData : ScriptableObject
{
    [Min(1)] public int Id;
    public string DisplayName;
    [TextArea] public string Description;
    public bool AllowDuplicates = true;
    public float CommonValue;
    public float RareValue;
    public float EpicValue;
    public float MythicalValue;
    public float LegendaryValue;
    [Min(0f)] public float BaseCooldown;
    [Min(0f)] public float CooldownReductionPerRarity;

    public float GetCooldown(EItemRarity rarity)
    {
        return Mathf.Max(0f, BaseCooldown - CooldownReductionPerRarity * (int)rarity);
    }

    public float GetValue(EItemRarity rarity)
    {
        switch (rarity)
        {
            case EItemRarity.Common: return CommonValue;
            case EItemRarity.Rare: return RareValue;
            case EItemRarity.Epic: return EpicValue;
            case EItemRarity.Mythical: return MythicalValue;
            case EItemRarity.Legendary: return LegendaryValue;
            default: return 0f;
        }
    }

    public virtual float GetTooltipValue(EItemRarity rarity, int stacks, ActorStats owner) => GetValue(rarity) * stacks;

    public abstract Enchant Create(Weapon weapon);
}
