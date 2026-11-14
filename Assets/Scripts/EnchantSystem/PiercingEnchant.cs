using UnityEngine;

public sealed class PiercingEnchant : Enchant
{
    public override void Initialize(EnchantData data, Weapon weapon)
    {
        base.Initialize(data, weapon);
        weapon.ModifyTargetLimit += IncreaseTargetLimit;
    }

    internal override void Detach()
    {
        if (Weapon != null)
            Weapon.ModifyTargetLimit -= IncreaseTargetLimit;
        base.Detach();
    }

    private void IncreaseTargetLimit(ref int count) => count += Mathf.Max(0, Mathf.FloorToInt(Value));
}
