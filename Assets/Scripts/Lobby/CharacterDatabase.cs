using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Characters/Database")]
public sealed class CharacterDatabase : ScriptableObject
{
    public CharacterConfig[] Characters = new CharacterConfig[0];
    public int Count => Characters.Length;
    public CharacterConfig Get(int index) => index >= 0 && index < Count ? Characters[index] : null;
    public int IndexOf(CharacterConfig character) => Array.IndexOf(Characters, character);

    public CharacterConfig Get(string name)
    {
        if (int.TryParse(name, out int index)) return Get(index);
        foreach (CharacterConfig character in Characters)
            if (character != null && (character.name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(character.DisplayName, name, StringComparison.OrdinalIgnoreCase))) return character;
        return null;
    }
}
