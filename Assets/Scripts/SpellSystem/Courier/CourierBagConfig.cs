using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Courier/Bag")]
public sealed class CourierBagConfig : SpellConfig
{
    [Min(0)] public int ExtraSlots = 2;
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<CourierBagSpell>();
}
