using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Knight/Second Wind")]
public sealed class KnightHealConfig : SpellConfig
{
    [Min(0f)] public float Healing = 20f;

    public override Spell AddSpell(GameObject owner)
    {
        return owner.AddComponent<KnightHeal>();
    }
}
