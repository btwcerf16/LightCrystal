using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Mathematician/Empirical Learning")]
public sealed class EmpiricalLearningConfig : SpellConfig
{
    public EffectData StackEffect;

    public override Spell AddSpell(GameObject owner) => owner.AddComponent<EmpiricalLearningSpell>();
}
