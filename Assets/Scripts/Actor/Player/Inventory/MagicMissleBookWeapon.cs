using Unity.Netcode;
using UnityEngine;

public sealed class MagicMissleBookWeapon : Weapon
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private MagicMissleProjectile _missilePrefab;
    [SerializeField] private MagicMissileBurstProjectile _burstPrefab;
    [SerializeField, Min(0f)] private float _sanityCost = 15f;
    [SerializeField, Min(1)] private int _magicMisslesCount = 3;
    [SerializeField, Min(0f)] private float _spreadAngle = 40f;
    [SerializeField, Min(0.1f)] private float _missileSpeed = 12f;
    [SerializeField, Min(0.1f)] private float _missileRange = 12f;
    [SerializeField, Min(0.1f)] private float _burstSpeed = 4f;
    [SerializeField, Min(0.1f)] private float _burstRange = 8f;
    [SerializeField] private LayerMask _targetMask;
    private ActorHand _hand;
    private ActorSanity _sanity;
    private bool _isAttacking;
    private bool _secondary;
    private bool _released;

    public override string DisplayName => "Magic Missile";
    public float MissileDamage => DamageCalculator.CalculateDamage(_damage, EDamageType.Magic, OwnerStats, null);
    public float BurstMissileDamage => MissileDamage / 3f;
    public override string TooltipDescription => base.TooltipDescription
        + $"\n\nЛКМ: {_magicMisslesCount} снаряда по {MissileDamage:0.#} маг. урона."
        + $"\nПКМ: медленный шар, выпускающий 8 снарядов по {BurstMissileDamage:0.#} маг. урона."
        + $"\nРассудок: {_sanityCost:0.#}. КД: {BaseCooldown * (1f - (OwnerStats != null ? OwnerStats.CooldownReduction.Value : 0f)):0.#} с.";

    public override void Equip()
    {
        base.Equip();
        _hand = GetComponentInParent<ActorHand>();
        _sanity = GetComponentInParent<ActorSanity>();
        _renderer.enabled = true;
        _hand.VisualRenderer.enabled = false;
        Projectile.TargetHit -= OnProjectileHit;
        if (IsServer)
            Projectile.TargetHit += OnProjectileHit;
    }

    public override void Drop()
    {
        CancelUse();
        Projectile.TargetHit -= OnProjectileHit;
        base.Drop();
        _renderer.enabled = false;
        if (_hand != null)
            _hand.VisualRenderer.enabled = true;
    }

    public override void BeginPrimaryUse()
    {
        if (IsEquipped && IsAvailable && !IsOwnerStunned && !_isAttacking)
            BeginAttack(false);
    }

    public override bool BeginSecondaryUse()
    {
        if (!IsEquipped || !IsAvailable || IsOwnerStunned || _isAttacking)
            return false;
        if (!IsServer)
        {
            BeginAttack(true);
            return true;
        }
        if (CooldownRemaining > 0f)
            return false;
        EffectHandler effects = OwnerStats.GetComponent<EffectHandler>();
        string error = effects.IsSilenced ? "Вы под действием безмолвия"
            : _sanity.CurrentSanity < _sanityCost ? "Недостаточно рассудка" : null;
        if (error != null)
        {
            OwnerStats.GetComponent<NetworkPlayerWallet>().ShowHint(error);
            return false;
        }
        return TryUse();
    }

    public override void Use()
    {
        _sanity.TrySpendSanity(_sanityCost);
        BeginAttack(true);
        NotifyStateChanged();
    }

    private void BeginAttack(bool secondary)
    {
        _secondary = secondary;
        _released = false;
        _isAttacking = true;
        _animator.SetTrigger("Shake");
    }

    public void ReleaseMissiles()
    {
        if (!IsServer || !IsEquipped || IsOwnerStunned || !_isAttacking || _released)
            return;
        _released = true;
        NetworkObjectPool pool = OwnerStats.GetComponent<NetworkObject>().NetworkManager.GetComponent<NetworkObjectPool>();
        Vector2 direction = GetProjectileDirection(_hand.AimDirection);
        ProjectileData data = new ProjectileData
        {
            Attacker = OwnerStats, Damage = _damage, DamageType = EDamageType.Magic,
            Direction = direction, Speed = _missileSpeed, LifeTime = _missileRange / _missileSpeed, LayerMask = _targetMask
        };
        if (_secondary)
        {
            MagicMissileBurstProjectile burst = pool.GetObject(_burstPrefab, _attackPoint.position, Quaternion.identity);
            burst.SetSource(this);
            data.Speed = _burstSpeed;
            data.LifeTime = _burstRange / _burstSpeed;
            burst.Launch(data);
            burst.GetComponent<NetworkObject>().Spawn();
            return;
        }
        for (int i = 0; i < _magicMisslesCount; i++)
        {
            float angle = _magicMisslesCount == 1 ? 0f : Mathf.Lerp(-_spreadAngle * 0.5f, _spreadAngle * 0.5f, (float)i / (_magicMisslesCount - 1));
            data.Direction = Quaternion.Euler(0f, 0f, angle) * direction;
            MagicMissleProjectile missile = pool.GetObject(_missilePrefab, _attackPoint.position, Quaternion.identity);
            missile.SetSource(this);
            missile.Launch(data);
            missile.GetComponent<NetworkObject>().Spawn();
        }
    }

    private void LateUpdate()
    {
        if (_hand == null)
            return;
        Vector3 scale = transform.localScale;
        scale.y = Mathf.Abs(scale.y) * (_hand.AimDirection.x < 0f ? -1f : 1f);
        transform.localScale = scale;
    }

    private void OnProjectileHit(Projectile projectile, ActorHealth target, float damage)
    {
        if (damage > 0f && projectile is MagicMissleProjectile missile && missile.Source == this)
            NotifyHit(target);
    }

    public void FinishAttack() => _isAttacking = false;
    public override void EndPrimaryUse() { }
    public override void EndSecondaryUse() { }
    public override void CancelUse()
    {
        _isAttacking = false;
        _animator.ResetTrigger("Shake");
        _animator.Play("Idle", 0, 0f);
    }

    protected override void OnDestroy()
    {
        Projectile.TargetHit -= OnProjectileHit;
        base.OnDestroy();
    }
}
