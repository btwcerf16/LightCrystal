using UnityEngine;

public abstract class SpellConfig : ScriptableObject
{
    public string SpellName;
    [TextArea] public string SpellDescription;
    [TextArea] public string StatsDescription;
    public bool IsPassive;
    [Min(0f)] public float CooldownTime;
    [Min(0f)] public float SanityCost;
    [Min(0f)] public float CastTime;
    public Sprite SpriteIcon;
    public string Description => TooltipText.Format(this, SpellDescription);
    public string Stats => TooltipText.Format(this, StatsDescription);
    public virtual float GetRange(ActorStats owner) => 0f;

    public abstract Spell AddSpell(GameObject owner);
}
