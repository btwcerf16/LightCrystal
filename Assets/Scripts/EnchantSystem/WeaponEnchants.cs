using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Weapon))]
public sealed class WeaponEnchants : MonoBehaviour
{
    [SerializeField] private EnchantData[] _definitions = Array.Empty<EnchantData>();
    private readonly List<Enchant> _enchants = new();
    private bool _initialized;
    private Weapon _weapon;
    private Weapon Owner
    {
        get
        {
            if (_weapon == null)
                _weapon = GetComponent<Weapon>();
            return _weapon;
        }
    }

    public int Capacity => GetCapacity(Owner.Rarity);

    public T GetEnchant<T>() where T : Enchant
    {
        foreach (Enchant enchant in _enchants)
            if (enchant is T match)
                return match;
        return null;
    }

    private void Awake() => EnsureEnchants();

    private void OnDestroy() => ClearEnchants();

    private static int GetCapacity(EItemRarity rarity)
    {
        if (rarity == EItemRarity.Legendary) return 3;
        if (rarity == EItemRarity.Mythical) return 2;
        return 1;
    }

    private void EnsureEnchants()
    {
        if (_initialized)
            return;
        _initialized = true;
        foreach (EnchantData data in _definitions)
        {
            if (data == null)
                continue;
            Enchant existing = _enchants.Find(enchant => enchant.Data == data);
            if (existing != null)
                existing.Stacks++;
            else
                _enchants.Add(data.Create(Owner));
        }
    }

    private void ClearEnchants()
    {
        foreach (Enchant enchant in _enchants)
        {
            if (enchant == null)
                continue;
            enchant.Detach();
            enchant.enabled = false;
            Destroy(enchant);
        }
        _enchants.Clear();
        _initialized = false;
    }

    public bool SetRarity(EItemRarity rarity) => SetEnchants(_definitions, rarity);

    public bool SetEnchants(IReadOnlyList<EnchantData> definitions)
    {
        return SetEnchants(definitions, Owner.Rarity);
    }

    public bool SetEnchants(IReadOnlyList<EnchantData> definitions, EItemRarity rarity)
    {
        if (rarity < EItemRarity.Common || rarity > EItemRarity.Legendary)
            return false;
        if (definitions == null || definitions.Count > GetCapacity(rarity))
            return false;
        HashSet<EnchantData> unique = new();
        bool same = rarity == Owner.Rarity && definitions.Count == _definitions.Length;
        for (int i = 0; i < definitions.Count; i++)
        {
            EnchantData data = definitions[i];
            if (data == null || data.Id <= 0 || (!unique.Add(data) && !data.AllowDuplicates))
                return false;
            if (i >= _definitions.Length || data != _definitions[i])
                same = false;
        }
        if (same)
        {
            EnsureEnchants();
            return true;
        }
        Dictionary<EnchantData, double> cooldowns = null;
        foreach (Enchant enchant in _enchants)
        {
            if (enchant.CooldownEndsAt <= Time.timeAsDouble)
                continue;

            cooldowns ??= new Dictionary<EnchantData, double>();
            cooldowns.Add(enchant.Data, enchant.CooldownEndsAt);
        }

        ClearEnchants();
        Owner.SetRarity(rarity);
        _definitions = new EnchantData[definitions.Count];
        for (int i = 0; i < definitions.Count; i++)
            _definitions[i] = definitions[i];
        EnsureEnchants();
        if (cooldowns != null)
            foreach (Enchant enchant in _enchants)
                if (cooldowns.TryGetValue(enchant.Data, out double endsAt))
                    enchant.CooldownEndsAt = endsAt;
        if (Owner.IsEquipped)
            foreach (Enchant enchant in _enchants)
                enchant.OnEquipped();
        Owner.NotifyStateChanged();
        return true;
    }

    public bool AddEnchant(EnchantData data)
    {
        EnchantData[] definitions = new EnchantData[_definitions.Length + 1];
        Array.Copy(_definitions, definitions, _definitions.Length);
        definitions[definitions.Length - 1] = data;
        return SetEnchants(definitions);
    }

    public bool RemoveLastEnchant()
    {
        if (_definitions.Length == 0)
            return false;
        EnchantData[] definitions = new EnchantData[_definitions.Length - 1];
        Array.Copy(_definitions, definitions, definitions.Length);
        return SetEnchants(definitions);
    }

    public EnchantState GetEnchantState()
    {
        return new EnchantState
        {
            First = _definitions.Length > 0 ? _definitions[0].Id : 0,
            Second = _definitions.Length > 1 ? _definitions[1].Id : 0,
            Third = _definitions.Length > 2 ? _definitions[2].Id : 0
        };
    }

}
