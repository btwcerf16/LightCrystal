using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Courier/Whistle")]
public sealed class CourierWhistleConfig : SpellConfig
{
    [Min(0f)] public float Radius = 10f;
    public override float GetRange(ActorStats owner) => Radius;
    public override Spell AddSpell(GameObject owner) => owner.AddComponent<CourierWhistleSpell>();
}
