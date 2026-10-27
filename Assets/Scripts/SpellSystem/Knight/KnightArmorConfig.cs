using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Knight/Tempered Armor")]
public sealed class KnightArmorConfig : SpellConfig
{
    [Range(0f, 1f)] public float Resistance = 0.05f;
    public float ResistancePercent => Resistance * 100f;

    private void OnValidate()
    {
        IsPassive = true;
        SanityCost = 0f;
        CooldownTime = 0f;
    }

    public override Spell AddSpell(GameObject owner)
    {
        return owner.AddComponent<KnightArmor>();
    }
}
