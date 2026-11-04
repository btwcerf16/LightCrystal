public sealed class CourierBagSpell : PassiveSpell
{
    protected override void Apply()
    {
        Inventory inventory = GetComponent<Inventory>();
        inventory.SetSize(inventory.Size + ((CourierBagConfig)SpellData).ExtraSlots);
    }

    protected override void Remove()
    {
        Inventory inventory = GetComponent<Inventory>();
        int size = inventory.Size - ((CourierBagConfig)SpellData).ExtraSlots;
        inventory.SetSize(size);
    }
}
