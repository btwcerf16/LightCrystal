using UnityEngine;

public sealed class ComboEnchant : Enchant
{
    private float _expiresAt;
    public int Count { get; private set; }

    public override void OnHit(IDamageable target)
    {
        Count += Stacks;
        _expiresAt = Time.time + ((ComboEnchantData)Data).ResetDelay;
        Weapon.OwnerStats.AttackSpeed.RemoveModifiersFrom(this);
        float bonus = Count * Data.GetValue(Weapon.Rarity);
        Weapon.OwnerStats.AttackSpeed.AddModifier(new StatModifier(bonus, StatModifierType.Flat, this));
    }

    private void Update()
    {
        if (Count > 0 && Time.time >= _expiresAt)
            ResetCombo();
    }

    private void ResetCombo()
    {
        Count = 0;
        if (Weapon != null && Weapon.OwnerStats != null)
            Weapon.OwnerStats.AttackSpeed.RemoveModifiersFrom(this);
    }

    public override void OnUnequipped() => ResetCombo();
    private void OnDisable() => ResetCombo();
}
