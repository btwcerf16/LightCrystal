using UnityEngine;

[CreateAssetMenu(menuName = "Enchants/Combo")]
public sealed class ComboEnchantData : EnchantData
{
    [Min(0.1f)] public float ResetDelay = 2f;

    public override Enchant Create(Weapon weapon)
    {
        Enchant enchant = weapon.gameObject.AddComponent<ComboEnchant>();
        enchant.Initialize(this, weapon);
        return enchant;
    }
}
