using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Nyx/Backstab")]
public sealed class NyxBackstabConfig : SpellConfig
{
    public NyxBackstabBuffData StackEffect;

    public override Spell AddSpell(GameObject owner) => owner.AddComponent<NyxBackstabSpell>();
}
