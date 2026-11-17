using UnityEngine;

public sealed class GladiatorTrophyItem : Item
{
    [SerializeField] private float _resistance = 0.12f;
    [SerializeField] private float _viewPenalty = 0.2f;
    [SerializeField] private int _targetPriorityBonus = 2;
    [SerializeField] private int _goldPerStep = 60;
    [SerializeField] private float _resistancePerStep = 0.02f;
    [SerializeField] private float _maximumGoldResistance = 0.1f;
    private ActorWallet _wallet;
    public float Resistance => _resistance;
    public float ViewPenalty => _viewPenalty;
    public int TargetPriorityBonus => _targetPriorityBonus;
    public int GoldPerStep => _goldPerStep;
    public float ResistancePerStep => _resistancePerStep;
    public float MaximumGoldResistance => _maximumGoldResistance;
    public override string DisplayName => "Трофей гладиатора";

    public override void Equip()
    {
        if (IsEquipped) return;
        base.Equip();
        if (!IsServer) return;
        _wallet = _ownerStats.GetComponent<ActorWallet>();
        _wallet.CurrencyChanged += RefreshResistance;
        _ownerStats.ViewDistance.AddModifier(new StatModifier(1f - _viewPenalty, StatModifierType.Multiplier, this));
        _ownerStats.ChangeTargetPriorityBonus(_targetPriorityBonus);
        RefreshResistance(_wallet.Currency);
    }

    private void RefreshResistance(int gold)
    {
        float bonus = Mathf.Min(_maximumGoldResistance, (gold / Mathf.Max(1, _goldPerStep)) * _resistancePerStep);
        _ownerStats.PhysicalResistance.RemoveModifiersFrom(this);
        _ownerStats.PhysicalResistance.AddModifier(new StatModifier(_resistance + bonus, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (!IsEquipped) return;
        if (_wallet != null) _wallet.CurrencyChanged -= RefreshResistance;
        if (IsServer)
        {
            _ownerStats.PhysicalResistance.RemoveModifiersFrom(this);
            _ownerStats.ViewDistance.RemoveModifiersFrom(this);
            _ownerStats.ChangeTargetPriorityBonus(-_targetPriorityBonus);
        }
        base.Drop();
    }

    public override void Use() { }

    private void OnDestroy() => Drop();
}
