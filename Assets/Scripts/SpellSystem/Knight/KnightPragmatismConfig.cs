using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Knight/Weapon Pragmatism")]
public sealed class KnightPragmatismConfig : SpellConfig
{
    public float[] DamageByRarity = { 0.3f, 0.2f, 0f, -0.1f, -0.2f };
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<KnightPragmatismSpell>();
}
