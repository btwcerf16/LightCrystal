using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Nyx/Backstab")]
public sealed class NyxBackstabConfig : SpellConfig
{
    [Min(0)] public int BonusKillGold = 3;

    public override Spell AddSpell(GameObject owner) => owner.AddComponent<NyxBackstabSpell>();
}
