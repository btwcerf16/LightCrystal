using UnityEngine;

public sealed class WeightedGloveItem : Item
{
    [SerializeField] private float _attackSpeedPenalty = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _stunChance = 0.25f;
    [SerializeField] private EffectData _stun;
    [SerializeField] private EffectData _release;
    private ActorArsenal _arsenal;
    private EffectHandler _effects;
    public override string DisplayName => "Утяжелительная перчатка";
    public float AttackSpeedPenalty => _attackSpeedPenalty;
    public float StunChancePercent => _stunChance * 100f;
    public EffectData Stun => _stun;
    public EffectData Release => _release;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (!IsServer)
            return;
        _effects = _ownerStats.GetComponent<EffectHandler>();
        _arsenal = _ownerStats.GetComponent<ActorArsenal>();
        _ownerStats.AttackSpeed.AddModifier(new StatModifier(-_attackSpeedPenalty, StatModifierType.Flat, this));
        if (_arsenal != null)
            _arsenal.MeleeHit += OnMeleeHit;
    }

    public override void Drop()
    {
        if (_arsenal != null)
            _arsenal.MeleeHit -= OnMeleeHit;
        if (IsServer)
            _ownerStats.AttackSpeed.RemoveModifiersFrom(this);
        base.Drop();
    }

    private void OnMeleeHit(IDamageable target)
    {
        if (target.IsDead || _effects.HasEffect(_release) || Random.value >= _stunChance)
            return;
        if (target is Component component)
            component.GetComponent<EffectHandler>()?.AddEffect(_stun);
    }

    public override void Use()
    {
        if (IsServer)
            _effects.AddEffect(_release);
    }
}
