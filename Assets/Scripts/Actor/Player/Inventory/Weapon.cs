using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public abstract class Weapon : Item
{
    [SerializeField] protected float _damage;
    [SerializeField] protected Transform _attackPoint;
    [SerializeField] protected Animator _animator;
    [SerializeField, Min(1)] private int _maxTargets = 1;
    [SerializeField, Min(0f)] private float _aimSwayAngle;
    [SerializeField] private EAttackType _primaryAttackType;
    [SerializeField] private EAttackType _secondaryAttackType;
    private Stat _animationSpeedStat;
    public EAttackType PrimaryAttackType => _primaryAttackType;
    public EAttackType SecondaryAttackType => _secondaryAttackType;

    public Vector2 GetProjectileDirection(Vector2 direction)
    {
        float stability = OwnerStats != null ? OwnerStats.Stability.Value : 0f;
        float angle = Mathf.Max(0f, _aimSwayAngle - stability) * Mathf.Sin(Time.time * 3f);
        return Quaternion.Euler(0f, 0f, angle) * direction;
    }
    public bool IsAvailable { get; private set; } = true;
    public int TargetLimit
    {
        get
        {
            EnsureEnchants();
            int count = _maxTargets;
            foreach (Enchant enchant in _enchants)
                count += enchant.ExtraTargets;
            return Mathf.Max(1, count);
        }
    }

    public virtual void SetAvailable(bool available)
    {
        if (IsAvailable == available)
            return;
        IsAvailable = available;
        StateChanged?.Invoke();
    }

    [SerializeField] private EnchantData[] _enchantDefinitions = new EnchantData[0];
    private readonly List<Enchant> _enchants = new();
    private bool _enchantsInitialized;
    private NetworkObject _networkOwner;

    public ActorStats OwnerStats { get; private set; }
    public int EnchantCapacity => GetCapacity(Rarity);
    public event Action StateChanged;
    protected void NotifyStateChanged() => StateChanged?.Invoke();
    public int ComboCount
    {
        get
        {
            EnsureEnchants();
            int count = 0;
            foreach (Enchant enchant in _enchants)
                if (enchant is ComboEnchant combo)
                    count += combo.Count;
            return count;
        }
    }

    private static int GetCapacity(EItemRarity rarity)
    {
        if (rarity == EItemRarity.Legendary) return 3;
        if (rarity == EItemRarity.Mythical) return 2;
        return 1;
    }

    private void EnsureEnchants()
    {
        if (_enchantsInitialized)
            return;
        _enchantsInitialized = true;
        foreach (EnchantData data in _enchantDefinitions)
        {
            if (data == null)
                continue;
            Enchant existing = _enchants.Find(enchant => enchant.Data == data);
            if (existing != null)
                existing.Stacks++;
            else
                _enchants.Add(data.Create(this));
        }
    }

    private void ClearEnchants()
    {
        foreach (Enchant enchant in _enchants)
        {
            enchant.OnUnequipped();
            enchant.enabled = false;
            Destroy(enchant);
        }
        _enchants.Clear();
        _enchantsInitialized = false;
    }

    public override bool SetRarity(EItemRarity rarity)
    {
        return SetEnchants(_enchantDefinitions, rarity);
    }

    public bool SetEnchants(IReadOnlyList<EnchantData> definitions)
    {
        return SetEnchants(definitions, Rarity);
    }

    public bool SetEnchants(IReadOnlyList<EnchantData> definitions, EItemRarity rarity)
    {
        if (rarity < EItemRarity.Common || rarity > EItemRarity.Legendary)
            return false;
        if (definitions == null || definitions.Count > GetCapacity(rarity))
            return false;
        HashSet<EnchantData> unique = new();
        bool same = rarity == Rarity && definitions.Count == _enchantDefinitions.Length;
        for (int i = 0; i < definitions.Count; i++)
        {
            EnchantData data = definitions[i];
            if (data == null || data.Id <= 0 || (!unique.Add(data) && !data.AllowDuplicates))
                return false;
            if (i >= _enchantDefinitions.Length || data != _enchantDefinitions[i])
                same = false;
        }
        if (same)
        {
            EnsureEnchants();
            return true;
        }
        ClearEnchants();
        base.SetRarity(rarity);
        _enchantDefinitions = new EnchantData[definitions.Count];
        for (int i = 0; i < definitions.Count; i++)
            _enchantDefinitions[i] = definitions[i];
        EnsureEnchants();
        if (IsEquipped)
            foreach (Enchant enchant in _enchants)
                enchant.OnEquipped();
        StateChanged?.Invoke();
        return true;
    }

    public bool AddEnchant(EnchantData data)
    {
        EnchantData[] definitions = new EnchantData[_enchantDefinitions.Length + 1];
        Array.Copy(_enchantDefinitions, definitions, _enchantDefinitions.Length);
        definitions[definitions.Length - 1] = data;
        return SetEnchants(definitions);
    }

    public EnchantState GetEnchantState()
    {
        return new EnchantState
        {
            First = _enchantDefinitions.Length > 0 ? _enchantDefinitions[0].Id : 0,
            Second = _enchantDefinitions.Length > 1 ? _enchantDefinitions[1].Id : 0,
            Third = _enchantDefinitions.Length > 2 ? _enchantDefinitions[2].Id : 0
        };
    }

    public void NotifyMeleeHit(IDamageable target)
    {
        NotifyHit(target);
        if (IsServer)
            OwnerStats.GetComponent<ActorArsenal>()?.NotifyMeleeHit(target);
    }

    public void NotifyHit(IDamageable target)
    {
        if (_networkOwner == null || !_networkOwner.IsSpawned || !_networkOwner.NetworkManager.IsServer)
            return;
        EnsureEnchants();
        foreach (Enchant enchant in _enchants)
            enchant.OnHit(target);
        if (target.IsDead && target is ActorHealth health)
            NotifyKill(health);
    }

    public void NotifyKill(ActorHealth target)
    {
        if (_networkOwner == null || !_networkOwner.IsSpawned || !_networkOwner.NetworkManager.IsServer)
            return;
        EnsureEnchants();
        foreach (Enchant enchant in _enchants)
            enchant.OnKill(target);
    }

    public float Damage => _damage;

    public override void Equip()
    {
        base.Equip();
        OwnerStats = GetComponentInParent<ActorStats>();
        _networkOwner = GetComponentInParent<NetworkObject>();
        EnsureEnchants();
        foreach (Enchant enchant in _enchants)
            enchant.OnEquipped();
        SetAttackType(_primaryAttackType);
    }

    public override void Drop()
    {
        foreach (Enchant enchant in _enchants)
            enchant.OnUnequipped();
        if (_animationSpeedStat != null)
            _animationSpeedStat.ValueChanged -= UpdateAnimationSpeed;
        _animationSpeedStat = null;
        UpdateAnimationSpeed(1f);
        base.Drop();
    }

    protected void SetAttackType(EAttackType attackType)
    {
        if (_animationSpeedStat != null)
            _animationSpeedStat.ValueChanged -= UpdateAnimationSpeed;
        _animationSpeedStat = null;
        if (OwnerStats != null)
        {
            _animationSpeedStat = OwnerStats.AttackSpeed;
            if (attackType == EAttackType.Magic)
                _animationSpeedStat = OwnerStats.CastSpeed;
            _animationSpeedStat.ValueChanged += UpdateAnimationSpeed;
        }
        UpdateAnimationSpeed(_animationSpeedStat != null ? _animationSpeedStat.Value : 1f);
    }

    private void UpdateAnimationSpeed(float value)
    {
        if (_animator != null)
            _animator.SetFloat("AttackSpeed", Mathf.Max(0f, value));
    }

    protected virtual void OnDestroy()
    {
        if (_animationSpeedStat != null)
            _animationSpeedStat.ValueChanged -= UpdateAnimationSpeed;
    }

    public abstract void BeginPrimaryUse();
    public abstract void EndPrimaryUse();
    public abstract bool BeginSecondaryUse();
    public abstract void EndSecondaryUse();
    public abstract void CancelUse();
    public virtual void SetAim(Vector2 aim) { }
}

