using Unity.Netcode;
using UnityEngine;

public sealed class NyxWeaponMasterySpell : ToggleSpell
{
    private ActorArsenal _arsenal;
    private PlayerNetworkInteractions _interactions;
    private NetworkObject _networkOwner;
    private ActorHand _hand;

    public override void Activate()
    {
        base.Activate();
        _arsenal = _owner.GetComponent<ActorArsenal>();
        _interactions = _owner.GetComponent<PlayerNetworkInteractions>();
        _networkOwner = _owner.GetComponent<NetworkObject>();
        _hand = _owner.GetComponentInChildren<ActorHand>();
        _interactions.WeaponUseStarted += OnWeaponUseStarted;
    }

    public override void Deactivate()
    {
        if (_interactions != null)
            _interactions.WeaponUseStarted -= OnWeaponUseStarted;
        base.Deactivate();
    }

    private void OnWeaponUseStarted(Weapon attackingWeapon, EAttackType attackType)
    {
        if (!IsToggled || attackType != EAttackType.Melee || _ownerEffects.IsDowned)
            return;

        Weapon otherWeapon = _arsenal.GetWeapon(1 - _arsenal.ActiveWeaponIndex);
        NyxWeaponMasteryConfig config = (NyxWeaponMasteryConfig)SpellData;
        if (otherWeapon == null || !otherWeapon.IsAvailable || otherWeapon.PrimaryAttackType != EAttackType.Melee || config.ShadowPrefab == null)
            return;

        if (!_ownerSanity.TrySpendSanity(GetReducedSanityCost(config.FollowUpSanityCost)))
            return;

        Vector2 direction = _hand.AimDirection;
        Vector3 position = _owner.transform.position + (Vector3)(Vector2.Perpendicular(direction) * 0.55f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        NetworkObjectPool pool = _networkOwner.NetworkManager.GetComponent<NetworkObjectPool>();
        WeaponAttackShadow shadow = pool.GetObject(config.ShadowPrefab, position, Quaternion.Euler(0f, 0f, angle));
        float physicalDamage = _ownerStats.PhysicalDamage.CalculateValue(attackingWeapon, otherWeapon.Damage);
        shadow.NetworkObject.Spawn(destroyWithScene: true);
        shadow.Play(_networkOwner, otherWeapon.ItemId, _hand.CurrentOffset.magnitude, physicalDamage);
    }
}
