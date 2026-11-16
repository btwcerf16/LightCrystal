using UnityEngine;

public sealed class NyxWeaponMasterySpell : ToggleSpell
{
    private ActorArsenal _arsenal;
    private ActorWallet _wallet;

    public override void Activate()
    {
        base.Activate();
        _arsenal = _owner.GetComponent<ActorArsenal>();
        _wallet = _owner.GetComponent<ActorWallet>();
        _arsenal.ResolveWeaponHit = ResolveHit;
    }

    public override void Deactivate()
    {
        if (_arsenal != null && _arsenal.ResolveWeaponHit == ResolveHit)
            _arsenal.ResolveWeaponHit = null;
        base.Deactivate();
    }

    private float ResolveHit(IDamageable target, float damage, EDamageType damageType, out bool critical, bool isMelee)
    {
        Enemy enemy = !IsToggled && !target.IsDead && target is ActorHealth health ? health.GetComponent<Enemy>() : null;
        Transform enemyTarget = enemy != null ? enemy.CurrentTarget : null;
        bool hiddenStrike = enemyTarget != null && !enemyTarget.IsChildOf(_owner.transform);
        float additionalCriticalChance = hiddenStrike ? 1f - _ownerStats.CriticalChance.Value : 0f;
        float dealt = DamageCalculator.DealCriticalDamage(target, damage, damageType, _ownerStats, out critical, isMelee, additionalCriticalChance);
        if (hiddenStrike && dealt > 0f && target.IsDead)
            _wallet.AddCurrency(((NyxWeaponMasteryConfig)SpellData).HiddenKillGold, enemy.transform.position);
        return dealt;
    }
}
