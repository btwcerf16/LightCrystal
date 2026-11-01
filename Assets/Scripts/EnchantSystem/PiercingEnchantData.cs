using UnityEngine;

[CreateAssetMenu(menuName = "Enchants/Piercing")]
public sealed class PiercingEnchantData : EnchantData
{
    public override Enchant Create(Weapon weapon)
    {
        Enchant enchant = weapon.gameObject.AddComponent<PiercingEnchant>();
        enchant.Initialize(this, weapon);
        return enchant;
    }
}
