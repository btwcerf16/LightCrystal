public sealed class KnightPragmatismSpell : PassiveSpell
{
    private ActorArsenal _arsenal;

    protected override void Apply()
    {
        _arsenal = GetComponent<ActorArsenal>();
        _arsenal.ActiveWeaponChanged += OnWeaponChanged;
        _arsenal.ContentsChanged += Refresh;
        Refresh();
    }

    private void OnWeaponChanged(int index, Weapon weapon) => Refresh();

    private void Refresh()
    {
        _ownerStats.PhysicalDamageMultiplier.RemoveModifiersFrom(this);
        Weapon weapon = _arsenal.ActiveWeapon;
        if (weapon == null)
            return;
        float[] bonuses = ((KnightPragmatismConfig)SpellData).DamageByRarity;
        int rarity = (int)weapon.Rarity;
        if (rarity >= 0 && rarity < bonuses.Length)
            _ownerStats.PhysicalDamageMultiplier.AddModifier(new StatModifier(bonuses[rarity], StatModifierType.Flat, this));
    }

    protected override void Remove()
    {
        _arsenal.ActiveWeaponChanged -= OnWeaponChanged;
        _arsenal.ContentsChanged -= Refresh;
        _ownerStats.PhysicalDamageMultiplier.RemoveModifiersFrom(this);
    }
}
