using UnityEngine;

public sealed class ThreeSidedCoinItem : Item
{
    [SerializeField] private float _damageBonus = 0.08f;
    [SerializeField] private float _bonusPerToss = 0.04f;
    [SerializeField] private int _maximumStacks = 5;
    [SerializeField] private float _bonusDuration = 5f;
    [SerializeField] private CoinCurseData[] _curses = new CoinCurseData[3];
    private int _stacks;
    private float _expiresAt;
    private float _nextToss;
    public float DamageBonus => _damageBonus;
    public float BonusPerToss => _bonusPerToss;
    public int MaximumStacks => _maximumStacks;
    public float BonusDuration => _bonusDuration;
    public float MaximumDamageBonus => _damageBonus + _bonusPerToss * _maximumStacks;
    public override string DisplayName => "Монета с тремя сторонами";

    public override void Equip()
    {
        if (IsEquipped) return;
        base.Equip();
        if (IsServer) RefreshDamage();
    }

    private void RefreshDamage()
    {
        float bonus = _damageBonus + _stacks * _bonusPerToss;
        _ownerStats.PhysicalDamageMultiplier.RemoveModifiersFrom(this);
        _ownerStats.MagicDamageMultiplier.RemoveModifiersFrom(this);
        _ownerStats.PhysicalDamageMultiplier.AddModifier(new StatModifier(bonus, StatModifierType.Flat, this));
        _ownerStats.MagicDamageMultiplier.AddModifier(new StatModifier(bonus, StatModifierType.Flat, this));
    }

    protected override bool CanUse()
    {
        if (Time.time < _nextToss || _curses.Length != 3) return false;
        foreach (CoinCurseData curse in _curses) if (curse == null) return false;
        return true;
    }

    public override void Use()
    {
        if (!IsServer) return;
        _ownerStats.GetComponent<EffectHandler>().AddEffect(_curses[Random.Range(0, _curses.Length)]);
        _stacks = Mathf.Min(_maximumStacks, _stacks + 1);
        _expiresAt = Time.time + _bonusDuration;
        _nextToss = Time.time + 0.5f;
        RefreshDamage();
    }

    private void Update()
    {
        if (!IsServer || !IsEquipped || _stacks == 0 || Time.time < _expiresAt) return;
        _stacks = 0;
        RefreshDamage();
    }

    public override void Drop()
    {
        if (IsServer)
        {
            _ownerStats.PhysicalDamageMultiplier.RemoveModifiersFrom(this);
            _ownerStats.MagicDamageMultiplier.RemoveModifiersFrom(this);
        }
        _stacks = 0;
        base.Drop();
    }
}
