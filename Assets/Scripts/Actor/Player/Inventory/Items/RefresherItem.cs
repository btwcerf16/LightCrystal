using UnityEngine;

public sealed class RefresherItem : Item
{
    [SerializeField, Range(0f, 1f)] private float _sanityPenalty = 0.5f;
    [SerializeField] private EffectData _refresh;
    public override string DisplayName => "Рефрешер";
    public float SanityPenaltyPercent => _sanityPenalty * 100f;
    public EffectData Refresh => _refresh;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (IsServer)
            _ownerStats.MaxSanity.AddModifier(new StatModifier(1f - _sanityPenalty, StatModifierType.Multiplier, this));
    }

    public override void Drop()
    {
        if (IsServer)
            _ownerStats.MaxSanity.RemoveModifiersFrom(this);
        base.Drop();
    }

    public override void Use()
    {
        if (IsServer)
            _ownerStats.GetComponent<EffectHandler>().AddEffect(_refresh);
    }
}
