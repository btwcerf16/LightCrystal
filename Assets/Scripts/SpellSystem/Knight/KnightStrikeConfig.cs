using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Knight/Shield Strike")]
public sealed class KnightStrikeConfig : SpellConfig
{
    [Min(0f)] public float Damage = 20f;
    [Min(0f)] public float Radius = 1.5f;

    public override Spell AddSpell(GameObject owner)
    {
        return owner.AddComponent<KnightStrike>();
    }
}
