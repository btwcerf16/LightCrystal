using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Local Player Settings")]
public sealed class LocalPlayerSettings : ScriptableObject
{
    [SerializeField] private string[] _inventoryBindings = new string[0];
    [SerializeField] private string[] _arsenalBindings = new string[0];
    [SerializeField] private string[] _spellBindings = new string[0];
    [SerializeField] private string[] _cancelCastBindings = new string[0];

    private const string VolumeKey = "Audio.MasterVolume";
    private const float DefaultVolume = 0.65f;
    private const string WindVolumeKey = "Audio.WindVolume";
    private const string NicknameKey = "Player.Nickname";
    private const string PerformanceStatsKey = "HUD.PerformanceStats";
    public const int NicknameLimit = 20;

    public static string Nickname => PlayerPrefs.GetString(NicknameKey);
    public static bool ShowPerformanceStats => PlayerPrefs.GetInt(PerformanceStatsKey) != 0;
    public static bool RecordRunStatistics { get; set; }

    public static string CleanNickname(string nickname)
    {
        var clean = new System.Text.StringBuilder(NicknameLimit);
        foreach (char character in nickname)
        {
            if (char.IsControl(character) || char.IsSurrogate(character) || character == '<' || character == '>')
                continue;
            if (clean.Length == NicknameLimit)
                break;
            clean.Append(character);
        }
        return clean.ToString().Trim();
    }

    public static void SetNickname(string nickname)
    {
        PlayerPrefs.SetString(NicknameKey, CleanNickname(nickname));
        PlayerPrefs.Save();
    }

    public static void SetShowPerformanceStats(bool show)
    {
        PlayerPrefs.SetInt(PerformanceStatsKey, show ? 1 : 0);
        PlayerPrefs.Save();
    }
    public static float WindVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(WindVolumeKey, 0.35f));
    public static event Action<float> WindVolumeChanged;

    public static void SetWindVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(WindVolumeKey, volume);
        WindVolumeChanged?.Invoke(volume);
    }

    private string _character = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadVolume()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
    }

    public static void SetVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
    }

    public void Load(int inventorySize, int arsenalSize, int spellSize = 0, string character = null)
    {
        if (character != null) _character = character;
        LoadBindings(ref _inventoryBindings, ESlotBindingType.Inventory, inventorySize);
        LoadBindings(ref _arsenalBindings, ESlotBindingType.Arsenal, arsenalSize);
        LoadBindings(ref _spellBindings, ESlotBindingType.Spell, spellSize);
        LoadBindings(ref _cancelCastBindings, ESlotBindingType.CancelCast, 1);
    }

    private void LoadBindings(ref string[] bindings, ESlotBindingType type, int size)
    {
        if (bindings.Length < size)
            Array.Resize(ref bindings, size);

        for (int i = 0; i < size; i++)
        {
            string defaultPath = string.IsNullOrEmpty(_character) ? bindings[i] : GetDefaultBinding(type, i);
            if (string.IsNullOrEmpty(defaultPath))
                defaultPath = GetDefaultBinding(type, i);

            string previous = PlayerPrefs.GetString("SlotBindings." + type + "." + i, defaultPath);
            bindings[i] = PlayerPrefs.GetString(PreferenceKey(type, i), previous);
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
        ResetBindings(_cancelCastBindings, ESlotBindingType.CancelCast);
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
            case ESlotBindingType.CancelCast: return _cancelCastBindings;
            default: return _spellBindings;
        }
    }

    private string GetDefaultBinding(ESlotBindingType type, int index)
    {
        if (type == ESlotBindingType.CancelCast)
            return "<Keyboard>/capsLock";
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
        return "SlotBindings." + (string.IsNullOrEmpty(_character) ? "" : _character + ".") + type + "." + index;
    }
}
