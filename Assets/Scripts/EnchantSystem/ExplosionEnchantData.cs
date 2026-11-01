using UnityEngine;

[CreateAssetMenu(menuName = "Enchants/Death Explosion")]
public sealed class ExplosionEnchantData : EnchantData
{
    [Min(0.1f)] public float Radius = 1.5f;

    public override float GetTooltipValue(EItemRarity rarity, int stacks, ActorStats owner)
    {
        return DamageCalculator.CalculateDamage(GetValue(rarity) * stacks, EDamageType.Magic, owner, null);
    }

    public override Enchant Create(Weapon weapon)
    {
        Enchant enchant = weapon.gameObject.AddComponent<ExplosionEnchant>();
        enchant.Initialize(this, weapon);
        return enchant;
    }
}
