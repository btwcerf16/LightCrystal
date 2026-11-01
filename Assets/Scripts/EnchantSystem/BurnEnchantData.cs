using UnityEngine;

[CreateAssetMenu(menuName = "Enchants/Burning")]
public sealed class BurnEnchantData : EnchantData
{
    public BurnEffectData Effect;

    public override float GetTooltipValue(EItemRarity rarity, int stacks, ActorStats owner)
    {
        return DamageCalculator.CalculateDamage(GetValue(rarity) * stacks, EDamageType.Magic, owner, null);
    }

    public override Enchant Create(Weapon weapon)
    {
        Enchant enchant = weapon.gameObject.AddComponent<BurnEnchant>();
        enchant.Initialize(this, weapon);
        return enchant;
    }
}
