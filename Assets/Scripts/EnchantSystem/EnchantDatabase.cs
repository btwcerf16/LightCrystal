using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Enchants/Database")]
public sealed class EnchantDatabase : ScriptableObject
{
    [SerializeField] private EnchantData[] _enchants = new EnchantData[0];

    public IReadOnlyList<EnchantData> Enchants => _enchants;

    public EnchantData GetEnchant(string name)
    {
        if (int.TryParse(name, out int id))
            return GetEnchant(id);
        foreach (EnchantData enchant in _enchants)
            if (enchant != null && (string.Equals(enchant.name, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(enchant.DisplayName, name, StringComparison.OrdinalIgnoreCase)))
                return enchant;
        return null;
    }

    public EnchantData GetEnchant(int id)
    {
        foreach (EnchantData enchant in _enchants)
            if (enchant != null && enchant.Id == id)
                return enchant;
        return null;
    }

    private void OnValidate()
    {
        HashSet<int> ids = new();
        foreach (EnchantData enchant in _enchants)
        {
            if (enchant == null || enchant.Id <= 0 || !ids.Add(enchant.Id))
                Debug.LogError("Enchant database contains an empty entry or an invalid/duplicate ID.", this);
        }
    }
}
