using UnityEngine;

public sealed class NyxBackstabSpell : PassiveSpell
{
    private ActorArsenal _arsenal;
    private ActorWallet _wallet;

    protected override void Apply()
    {
        _arsenal = _owner.GetComponent<ActorArsenal>();
        _wallet = _owner.GetComponent<ActorWallet>();
        _arsenal.ResolveWeaponHit = ResolveHit;
    }

    protected override void Remove()
    {
        if (_arsenal != null && _arsenal.ResolveWeaponHit == ResolveHit)
            _arsenal.ResolveWeaponHit = null;
    }

    private float ResolveHit(IDamageable target, float damage, EDamageType damageType, out bool critical, bool isMelee)
    {
        Enemy enemy = !target.IsDead && target is ActorHealth health ? health.GetComponent<Enemy>() : null;
        Transform enemyTarget = enemy != null ? enemy.CurrentTarget : null;
        bool backstab = enemyTarget != null && !enemyTarget.IsChildOf(_owner.transform);
        float additionalCriticalChance = backstab ? 1f - _ownerStats.CriticalChance.Value : 0f;
        float dealt = DamageCalculator.DealCriticalDamage(target, damage, damageType, _ownerStats, out critical, isMelee, additionalCriticalChance);
        if (backstab && dealt > 0f && target.IsDead)
            _wallet.AddCurrency(((NyxBackstabConfig)SpellData).BonusKillGold, enemy.transform.position);
        return dealt;
    }
}
