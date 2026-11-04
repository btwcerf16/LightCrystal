using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Knight/Adaptive Stance")]
public sealed class KnightStanceConfig : SpellConfig
{
    public float ResistancePerStep = 0.03f;
    public float AttackSpeedPerStep = 0.1f;
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<KnightStanceSpell>();
}
