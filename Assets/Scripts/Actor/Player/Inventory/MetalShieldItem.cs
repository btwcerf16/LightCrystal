using System.Collections.Generic;
using UnityEngine;

public sealed class MetalShieldItem : Item
{
    [SerializeField] private float _physicalResistance = 0.1f;
    [SerializeField] private float _radius = 3f;
    [SerializeField] private float _pushStrength = 12f;
    [SerializeField] private float _pushDuration = 0.25f;
    private readonly HashSet<Enemy> _targets = new HashSet<Enemy>();
    public override float GetRange(ActorStats owner) => _radius;
    public override string DisplayName => "Металлический щит";
    public float PhysicalResistancePercent => _physicalResistance * 100f;
    public float PushRadius => _radius;

    public override void Equip()
    {
        if (IsEquipped)
            return;
        base.Equip();
        if (IsServer)
            _ownerStats.PhysicalResistance.AddModifier(new StatModifier(_physicalResistance, StatModifierType.Flat, this));
    }

    public override void Drop()
    {
        if (IsServer)
            _ownerStats.PhysicalResistance.RemoveModifiersFrom(this);
        base.Drop();
    }

    public override void Use()
    {
        if (!IsServer)
            return;
        _targets.Clear();
        Vector2 center = _ownerStats.transform.position;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, _radius))
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null || !_targets.Add(enemy))
                continue;
            Vector2 direction = (Vector2)enemy.transform.position - center;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.up;
            enemy.ApplyKnockback(direction, _pushStrength, _pushDuration);
        }
    }
}
