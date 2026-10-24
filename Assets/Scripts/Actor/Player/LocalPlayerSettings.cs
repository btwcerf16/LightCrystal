using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Local Player Settings")]
public sealed class LocalPlayerSettings : ScriptableObject
{
    [SerializeField] private string[] _inventoryBindings = new string[0];
    [SerializeField] private string[] _arsenalBindings = new string[0];

    public void Load(int inventorySize, int arsenalSize)
    {
        LoadBindings(ref _inventoryBindings, false, inventorySize);
        LoadBindings(ref _arsenalBindings, true, arsenalSize);
    }

    private void LoadBindings(ref string[] bindings, bool isWeapon, int size)
    {
        if (bindings.Length < size)
            Array.Resize(ref bindings, size);

        for (int i = 0; i < size; i++)
        {
            string defaultPath = bindings[i];
            if (string.IsNullOrEmpty(defaultPath))
                defaultPath = GetDefaultBinding(isWeapon, i);

            bindings[i] = PlayerPrefs.GetString(PreferenceKey(isWeapon, i), defaultPath);
        }
    }

    public string GetBinding(bool isWeapon, int index)
    {
        string[] bindings = isWeapon ? _arsenalBindings : _inventoryBindings;
        return index >= 0 && index < bindings.Length ? bindings[index] : "";
    }

    public void SetBinding(bool isWeapon, int index, string path)
    {
        string[] bindings = isWeapon ? _arsenalBindings : _inventoryBindings;
        if (index < 0 || index >= bindings.Length)
            return;

        bindings[index] = path;
        PlayerPrefs.SetString(PreferenceKey(isWeapon, index), path);
        PlayerPrefs.Save();
    }

    public void ResetBindings()
    {
        ResetBindings(_inventoryBindings, false);
        ResetBindings(_arsenalBindings, true);
        PlayerPrefs.Save();
    }

    private void ResetBindings(string[] bindings, bool isWeapon)
    {
        for (int i = 0; i < bindings.Length; i++)
        {
            bindings[i] = GetDefaultBinding(isWeapon, i);
            PlayerPrefs.SetString(PreferenceKey(isWeapon, i), bindings[i]);
        }
    }

    private string GetDefaultBinding(bool isWeapon, int index)
    {
        if (isWeapon)
            return index < 12 ? "<Keyboard>/f" + (index + 1) : "";
        return index < 10 ? "<Keyboard>/" + ((index + 1) % 10) : "";
    }

    private string PreferenceKey(bool isWeapon, int index)
    {
        return "SlotBindings." + (isWeapon ? "Arsenal." : "Inventory.") + index;
    }
}
