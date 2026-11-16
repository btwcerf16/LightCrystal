using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Nyx/Weapon Mastery")]
public sealed class NyxWeaponMasteryConfig : SpellConfig
{
    [Min(0)] public int HiddenKillGold = 3;

    public override Spell AddSpell(GameObject owner) => owner.AddComponent<NyxWeaponMasterySpell>();
}
