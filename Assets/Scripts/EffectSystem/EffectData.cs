using UnityEngine;

public abstract class EffectData : ScriptableObject
{
    public string EffectName;
    [Min(0.01f)] public float EffectDuration = 5f;
    [TextArea] public string EffectDescription;
    [TextArea] public string EffectStats;
    public Sprite SpriteIcon;
    public Color EffectColor = Color.white;
    public bool IsBuff;
    public bool CanPurged = true;
    public bool Stackable;
    public bool RestartOnReapply = true;

    public abstract Effect CreateEffect(GameObject owner);

    public string FormatText(string template)
    {
        return TooltipText.Format(this, template);
    }
}
