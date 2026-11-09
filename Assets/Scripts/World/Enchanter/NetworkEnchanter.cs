using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Enchanter))]
public sealed class NetworkEnchanter : NetworkBehaviour
{
    [SerializeField] private EnchantDatabase _enchantDatabase;
    [SerializeField] private Sprite[] _runeSprites;
    [SerializeField] private bool _announceAppearance = true;
    private readonly NetworkVariable<int> _runeSpriteIndex = new();
    private readonly NetworkVariable<int> _enchantId = new();
    private readonly NetworkVariable<bool> _runeBroken = new();
    private Enchanter _enchanter;

    private void Awake()
    {
        _enchanter = GetComponent<Enchanter>();
    }

    public override void OnNetworkSpawn()
    {
        if (_enchantDatabase == null || _enchantDatabase.Enchants.Count == 0 || _runeSprites == null || _runeSprites.Length == 0)
        {
            Debug.LogError("Чарователю нужны база зачарований и спрайты рун.", this);
            return;
        }
        if (IsServer)
        {
            _runeSpriteIndex.Value = Random.Range(0, _runeSprites.Length);
            EnchantData enchant = _enchantDatabase.Enchants[Random.Range(0, _enchantDatabase.Enchants.Count)];
            if (enchant == null)
            {
                Debug.LogError("В базе зачарований найдена пустая запись.", this);
                return;
            }
            _enchantId.Value = enchant.Id;
        }
        if (_runeSpriteIndex.Value < 0 || _runeSpriteIndex.Value >= _runeSprites.Length)
        {
            Debug.LogError("Получен неизвестный спрайт руны.", this);
            return;
        }
        _enchanter.Initialize(_runeSprites[_runeSpriteIndex.Value], _enchantDatabase.GetEnchant(_enchantId.Value));
        if (_enchanter.Rune == null)
            return;
        _runeBroken.OnValueChanged += ApplyRuneState;
        ApplyRuneState(false, _runeBroken.Value);
        if (IsServer)
            _enchanter.Rune.Died += OnRuneBroken;
        if (IsServer && _announceAppearance)
            PlayerHUD.NotifyPlayers(NetworkManager, "Появился чарователь", new Color32(194, 163, 224, 255));
    }

    private void OnRuneBroken()
    {
        _runeBroken.Value = true;
    }

    private void ApplyRuneState(bool previous, bool broken)
    {
        _enchanter.Rune.SetBroken(broken);
    }

    public override void OnNetworkDespawn()
    {
        _runeBroken.OnValueChanged -= ApplyRuneState;
        if (_enchanter.Rune != null)
            _enchanter.Rune.Died -= OnRuneBroken;
    }
}
