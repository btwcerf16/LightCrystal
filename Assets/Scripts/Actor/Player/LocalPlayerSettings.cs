using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Local Player Settings")]
public sealed class LocalPlayerSettings : ScriptableObject
{
    [SerializeField] private string[] _inventoryBindings = new string[0];
    [SerializeField] private string[] _arsenalBindings = new string[0];
    [SerializeField] private string[] _spellBindings = new string[0];

    public void Load(int inventorySize, int arsenalSize, int spellSize = 0)
    {
        LoadBindings(ref _inventoryBindings, ESlotBindingType.Inventory, inventorySize);
        LoadBindings(ref _arsenalBindings, ESlotBindingType.Arsenal, arsenalSize);
        LoadBindings(ref _spellBindings, ESlotBindingType.Spell, spellSize);
    }

    private void LoadBindings(ref string[] bindings, ESlotBindingType type, int size)
    {
        if (bindings.Length < size)
            Array.Resize(ref bindings, size);

        for (int i = 0; i < size; i++)
        {
            string defaultPath = bindings[i];
            if (string.IsNullOrEmpty(defaultPath))
                defaultPath = GetDefaultBinding(type, i);

            bindings[i] = PlayerPrefs.GetString(PreferenceKey(type, i), defaultPath);
        }
    }

    public string GetBinding(bool isWeapon, int index)
    {
        return GetBinding(isWeapon ? ESlotBindingType.Arsenal : ESlotBindingType.Inventory, index);
    }

    public string GetBinding(ESlotBindingType type, int index)
    {
        string[] bindings = GetBindings(type);
        return index >= 0 && index < bindings.Length ? bindings[index] : "";
    }

    public void SetBinding(bool isWeapon, int index, string path)
    {
        SetBinding(isWeapon ? ESlotBindingType.Arsenal : ESlotBindingType.Inventory, index, path);
    }

    public void SetBinding(ESlotBindingType type, int index, string path)
    {
        string[] bindings = GetBindings(type);
        if (index < 0 || index >= bindings.Length)
            return;

        bindings[index] = path;
        PlayerPrefs.SetString(PreferenceKey(type, index), path);
        PlayerPrefs.Save();
    }

    public void ResetBindings()
    {
        ResetBindings(_inventoryBindings, ESlotBindingType.Inventory);
        ResetBindings(_arsenalBindings, ESlotBindingType.Arsenal);
        ResetBindings(_spellBindings, ESlotBindingType.Spell);
        PlayerPrefs.Save();
    }

    private void ResetBindings(string[] bindings, ESlotBindingType type)
    {
        for (int i = 0; i < bindings.Length; i++)
        {
            bindings[i] = GetDefaultBinding(type, i);
            PlayerPrefs.SetString(PreferenceKey(type, i), bindings[i]);
        }
    }

    private string[] GetBindings(ESlotBindingType type)
    {
        switch (type)
        {
            case ESlotBindingType.Inventory: return _inventoryBindings;
            case ESlotBindingType.Arsenal: return _arsenalBindings;
            default: return _spellBindings;
        }
    }

    private string GetDefaultBinding(ESlotBindingType type, int index)
    {
        if (type == ESlotBindingType.Spell)
        {
            string[] keys = { "r", "f", "c", "v" };
            return index < keys.Length ? "<Keyboard>/" + keys[index] : "";
        }
        if (type == ESlotBindingType.Arsenal)
            return index < 12 ? "<Keyboard>/f" + (index + 1) : "";
        return index < 10 ? "<Keyboard>/" + ((index + 1) % 10) : "";
    }

    private string PreferenceKey(ESlotBindingType type, int index)
    {
        return "SlotBindings." + type + "." + index;
    }
}
