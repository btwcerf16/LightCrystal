using Unity.Netcode;
using UnityEngine;

public sealed class ComboEnchant : Enchant
{
    private float _expiresAt;
    private ComboCounter _counter;
    private int _count;

    public override void OnHit(IDamageable target, EAttackType attackType, UnityEngine.Vector2 impactPoint)
    {
        ComboEnchantData combo = (ComboEnchantData)Data;
        _count += Stacks;
        _expiresAt = Time.time + combo.ResetDelay;
        Weapon.OwnerStats.AttackSpeed.RemoveModifiersFrom(this);
        float bonus = _count * combo.GetValue(Weapon.Rarity);
        Weapon.OwnerStats.AttackSpeed.AddModifier(new StatModifier(bonus, StatModifierType.Flat, this));
        UpdateCounter(combo.CounterPrefab);
    }

    private void UpdateCounter(ComboCounter prefab)
    {
        if (prefab == null || !Weapon.OwnerStats.TryGetComponent(out NetworkObject owner)
            || !owner.IsSpawned || !owner.NetworkManager.IsServer || owner.NetworkManager.ShutdownInProgress)
            return;

        ulong ownerId = owner.OwnerClientId;
        if (_counter != null && _counter.OwnerClientId != ownerId)
            DestroyCounter();
        if (_counter == null)
        {
            _counter = Instantiate(prefab);
            _counter.NetworkObject.CheckObjectVisibility = clientId => clientId == ownerId;
            _counter.NetworkObject.SpawnWithOwnership(ownerId, destroyWithScene: true);
        }
        _counter.SetCount(_count);
    }

    private void Update()
    {
        if (_count > 0 && Time.time >= _expiresAt)
            ResetCombo();
    }

    private void ResetCombo()
    {
        if (_count == 0)
            return;
        _count = 0;
        if (Weapon != null && Weapon.OwnerStats != null)
            Weapon.OwnerStats.AttackSpeed.RemoveModifiersFrom(this);
        if (_counter != null && _counter.IsSpawned)
            _counter.SetCount(0);
    }

    public override void OnUnequipped() => ResetCombo();
    private void OnDisable() => ResetCombo();
    private void OnDestroy() => DestroyCounter();

    private void DestroyCounter()
    {
        if (_counter != null && _counter.IsSpawned && !_counter.NetworkManager.ShutdownInProgress)
            _counter.NetworkObject.Despawn();
        _counter = null;
    }
}
