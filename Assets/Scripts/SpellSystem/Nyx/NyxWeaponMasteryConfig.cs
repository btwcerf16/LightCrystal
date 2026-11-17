using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Nyx/Shadow Attack")]
public sealed class NyxWeaponMasteryConfig : SpellConfig
{
    [Min(0f)] public float FollowUpSanityCost = 5f;
    public WeaponAttackShadow ShadowPrefab;

    public override Spell AddSpell(GameObject owner) => owner.AddComponent<NyxWeaponMasterySpell>();
}
