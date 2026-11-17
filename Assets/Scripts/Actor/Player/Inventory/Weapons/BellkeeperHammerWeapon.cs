using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class BellkeeperHammerWeapon : Weapon
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private EffectData _stun;
    [SerializeField, Min(0f)] private float _sanityCost = 15f;
    [SerializeField, Min(0.01f)] private float _attackRadius = 0.55f;
    private ActorSanity _sanity;
    private EffectHandler _effects;
    private bool _isSlam;
    private bool _impactPlayed;
    private readonly HashSet<IDamageable> _hitTargets = new();

    public override string DisplayName => "Молот звонаря";
    private float CurrentDamage => PhysicalDamage;
    public float HitDamage => DamageCalculator.CalculateDamage(CurrentDamage, EDamageType.Physical, OwnerStats, null);
    public float AbilityCooldown => GetReducedCooldown(BaseCooldown);
    public override string GetWeaponTooltip(bool detailed) => base.GetWeaponTooltip(detailed)
        + $"\n\nУрон ЛКМ и ПКМ: {FormatDamage(_damage, EDamageType.Physical, detailed, true)}"
        + $"\nСкорость атаки: {FormatAttackSpeed(PrimaryAttackType, detailed)}\nЦелей за удар: {TargetLimit}"
        + $"\nПКМ: стан {_stun.EffectDuration:0.#} с, рассудок {_sanityCost:0.#}"
        + $"\nКулдаун ПКМ: {FormatTooltipValue(BaseCooldown, AbilityCooldown, detailed)} с";

    public override void Equip()
    {
        base.Equip();
        _sanity = GetComponentInParent<ActorSanity>();
        _effects = GetComponentInParent<EffectHandler>();
        if (!_bonusApplied && OwnerStats != null && OwnerNetworkObject != null && OwnerNetworkObject.IsSpawned && OwnerNetworkObject.NetworkManager.IsServer)
        {
            OwnerStats.PhysicalDamage.AddModifier(new StatModifier(_damage, StatModifierType.Flat, this));
            _bonusApplied = true;
        }
        if (_hand != null)
            _hand.VisualRenderer.enabled = false;
        _renderer.enabled = true;
    }

    public override void Drop()
    {
        CancelUse();
        if (_bonusApplied && OwnerStats != null)
            OwnerStats.PhysicalDamage.RemoveModifiersFrom(this);
        _bonusApplied = false;
        base.Drop();
        _renderer.enabled = false;
        if (_hand != null)
            _hand.VisualRenderer.enabled = true;
    }

    private void BeginAttack(string trigger, bool slam)
    {
        SetAttackType(slam ? SecondaryAttackType : PrimaryAttackType);
        _isAttacking = true;
        _isSlam = slam;
        _impactPlayed = false;
        _hitTargets.Clear();
        _animator.SetTrigger(trigger);
    }

    public override bool BeginPrimaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking)
            return false;
        BeginAttack("Swing", false);
        return true;
    }

    public override bool BeginSecondaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking || OwnerNetworkObject == null || !OwnerNetworkObject.IsSpawned)
            return false;
        if (!OwnerNetworkObject.NetworkManager.IsServer)
        {
            BeginAttack("Slam", true);
            return true;
        }
        if (CooldownRemaining > 0f)
            return false;
        string error = _effects != null && _effects.IsSilenced ? "Вы под действием безмолвия"
            : _sanity == null || _sanity.CurrentSanity < _sanityCost ? "Недостаточно рассудка" : null;
        if (error != null)
        {
            OwnerNetworkObject.GetComponent<NetworkPlayerWallet>().ShowHint(error);
            return false;
        }
        return TryUse();
    }

    public override void Use()
    {
        _sanity.TrySpendSanity(_sanityCost);
        BeginAttack("Slam", true);
        NotifyStateChanged();
    }

    private void LateUpdate()
    {
        if (_hand == null)
            return;
        Vector3 scale = transform.localScale;
        float side = _hand.AimDirection.x < 0f ? -1f : 1f;
        if (Mathf.Sign(scale.y) != side)
        {
            scale.y = Mathf.Abs(scale.y) * side;
            transform.localScale = scale;
        }
    }

    private void PlayImpact()
    {
        if (_impactPlayed)
            return;
        _impactPlayed = true;
        OwnerNetworkObject.GetComponent<NetworkPlayerSetup>().PlayWeaponImpact(_isSlam);
    }

    public void Strike()
    {
        if (!_isAttacking || IsOwnerStunned || !IsEquipped || OwnerNetworkObject == null || !OwnerNetworkObject.IsSpawned || !OwnerNetworkObject.NetworkManager.IsServer)
            return;
        if (_isSlam)
            PlayImpact();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(_attackPoint.position, _attackRadius))
        {
            if (_hitTargets.Count >= TargetLimit)
                break;
            if (hit.transform.root == OwnerNetworkObject.transform.root)
                continue;
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || _hitTargets.Contains(target))
                continue;
            if (ActorArsenal.DealWeaponDamage(target, CurrentDamage, EDamageType.Physical, OwnerStats, isMelee: true) <= 0f)
                continue;
            _hitTargets.Add(target);
            PlayImpact();
            if (_isSlam && !target.IsDead)
                hit.GetComponentInParent<EffectHandler>()?.AddEffect(_stun);
            NotifyMeleeHit(target, hit.ClosestPoint(_attackPoint.position));
        }
    }

    public void FinishAttack() => _isAttacking = false;
    public override void EndPrimaryUse() { }
    public override void EndSecondaryUse() { }
    public override void CancelUse()
    {
        _isAttacking = false;
        _animator.ResetTrigger("Swing");
        _animator.ResetTrigger("Slam");
        _animator.Play("Idle", 0, 0f);
    }
}
