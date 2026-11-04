using System.Collections;
using UnityEngine;

public sealed class IncenseGarlicItem : Item
{
    [SerializeField] private float _costReduction = 0.08f;
    [SerializeField] private float _madnessResistance = 0.1f;
    [SerializeField] private float _delay = 3f;
    [SerializeField] private float _activeCooldown = 45f;
    [SerializeField] private RitualBuffData _ritual;
    public float CostReduction => _costReduction;
    public float MadnessResistance => _madnessResistance;
    public float Delay => _delay;
    public float Duration => _ritual != null ? _ritual.EffectDuration : 0f;
    public float RestoreOnExpiry => _ritual != null ? _ritual.RestoreSanityOnExpiry : 0f;
    public override string DisplayName => "Чеснок для воскуривания";
    public override float BaseCooldown => _activeCooldown;

    public override void Equip()
    {
        if (IsEquipped) return;
        base.Equip();
        if (!IsServer) return;
        _ownerStats.SanityCostReduction.AddModifier(new StatModifier(_costReduction, StatModifierType.Flat, this));
        _ownerStats.MadnessResistance.AddModifier(new StatModifier(_madnessResistance, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        StopAllCoroutines();
        if (IsServer)
        {
            _ownerStats.SanityCostReduction.RemoveModifiersFrom(this);
            _ownerStats.MadnessResistance.RemoveModifiersFrom(this);
        }
        base.Drop();
    }

    protected override bool CanUse() => _ritual != null;
    public override void Use()
    {
        if (IsServer) StartCoroutine(PerformRitual());
    }

    private IEnumerator PerformRitual()
    {
        yield return new WaitForSeconds(_delay);
        if (IsEquipped && !_ownerStats.GetComponent<ActorHealth>().IsDead)
            _ownerStats.GetComponent<EffectHandler>().AddEffect(_ritual);
    }
}
