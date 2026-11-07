using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class BellkeeperHammerWeapon : Weapon
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private EffectData _stun;
    [SerializeField, Min(0f)] private float _sanityCost = 15f;
    [SerializeField, Min(0.01f)] private float _attackRadius = 0.55f;
    private ActorHand _hand;
    private ActorSanity _sanity;
    private EffectHandler _effects;
    private NetworkObject _owner;
    private bool _isAttacking;
    private bool _isSlam;
    private bool _impactPlayed;
    private readonly HashSet<IDamageable> _hitTargets = new();

    public override string DisplayName => "Молот звонаря";
    private float CurrentDamage => OwnerStats != null ? OwnerStats.PhysicalDamage.Value : _damage;
    public float HitDamage => DamageCalculator.CalculateDamage(CurrentDamage, EDamageType.Physical, OwnerStats, null);
    public float AbilityCooldown => BaseCooldown * (1f - (OwnerStats != null ? OwnerStats.CooldownReduction.Value : 0f));
    public override string TooltipDescription => base.TooltipDescription
        + $"\n\nУрон: {HitDamage:0.#}\nЦелей за удар: {TargetLimit}\nСтан: {_stun.EffectDuration:0.#} с"
        + $"\nРассудок: {_sanityCost:0.#}\nКулдаун: {AbilityCooldown:0.#} с";

    public override void Equip()
    {
        base.Equip();
        _hand = GetComponentInParent<ActorHand>();
        _sanity = GetComponentInParent<ActorSanity>();
        _effects = GetComponentInParent<EffectHandler>();
        _owner = GetComponentInParent<NetworkObject>();
        if (!_bonusApplied && OwnerStats != null && _owner != null && _owner.IsSpawned && _owner.NetworkManager.IsServer)
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
        _isAttacking = true;
        _isSlam = slam;
        _impactPlayed = false;
        _hitTargets.Clear();
        _animator.SetTrigger(trigger);
    }

    public override void BeginPrimaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking)
            return;
        BeginAttack("Swing", false);
    }

    public override bool BeginSecondaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking || _owner == null || !_owner.IsSpawned)
            return false;
        if (!_owner.NetworkManager.IsServer)
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
            _owner.GetComponent<NetworkPlayerWallet>().ShowHint(error);
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
        _owner.GetComponent<NetworkPlayerSetup>().PlayWeaponImpact(_isSlam);
    }

    public void Strike()
    {
        if (!_isAttacking || IsOwnerStunned || !IsEquipped || _owner == null || !_owner.IsSpawned || !_owner.NetworkManager.IsServer)
            return;
        if (_isSlam)
            PlayImpact();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(_attackPoint.position, _attackRadius))
        {
            if (_hitTargets.Count >= TargetLimit)
                break;
            if (hit.transform.root == transform.root)
                continue;
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || _hitTargets.Contains(target))
                continue;
            if (DamageCalculator.DealCriticalDamage(target, CurrentDamage, EDamageType.Physical, OwnerStats) <= 0f)
                continue;
            _hitTargets.Add(target);
            PlayImpact();
            if (_isSlam && !target.IsDead)
                hit.GetComponentInParent<EffectHandler>()?.AddEffect(_stun);
            NotifyMeleeHit(target);
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
