using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Enchanter))]
public sealed class NetworkEnchanter : NetworkBehaviour
{
    [SerializeField] private EnchantDatabase _enchantDatabase;
    [SerializeField] private Sprite[] _runeSprites;
    [SerializeField] private Sprite[] _charredRuneSprites;
    [SerializeField] private bool _announceAppearance = true;
    [SerializeField, Min(0f)] private float _lifetime = 60f;
    private NetworkList<NetworkRuneState> _runes;
    private readonly NetworkVariable<int> _eventLevel = new(2);
    private Enchanter _enchanter;
    private float _capacity = 50f;
    private double _expiresAt;
    public int EventLevel => _eventLevel.Value;
    public void SetCapacity(float capacity) => _capacity = Mathf.Clamp(capacity, 0f, 100f);

    private void Awake()
    {
        _enchanter = GetComponent<Enchanter>();
        _runes = new NetworkList<NetworkRuneState>();
    }

    public override void OnNetworkSpawn()
    {
        if (_enchantDatabase == null || _enchantDatabase.Enchants.Count == 0 || _runeSprites == null || _runeSprites.Length == 0 || _charredRuneSprites == null || _charredRuneSprites.Length != _runeSprites.Length)
        {
            Debug.LogError("Чарователю нужны база зачарований и спрайты рун", this);
            return;
        }
        if (IsServer)
        {
            if (_capacity < 50f)
                _eventLevel.Value = 1;
            else if (_capacity < 80f)
                _eventLevel.Value = 2;
            else
                _eventLevel.Value = 3;
            _runes.Clear();
            for (int i = 0; i < EventLevel; i++)
            {
                bool removesEnchant = EventLevel > 1 && i == EventLevel - 1;
                EnchantData enchant = removesEnchant ? null : _enchantDatabase.Enchants[Random.Range(0, _enchantDatabase.Enchants.Count)];
                if (!removesEnchant && enchant == null)
                {
                    Debug.LogError("В базе зачарований найдена пустая запись", this);
                    return;
                }
                _runes.Add(new NetworkRuneState { EnchantId = removesEnchant ? 0 : enchant.Id, SpriteIndex = Random.Range(0, _runeSprites.Length) });
            }
            _expiresAt = _lifetime > 0f ? NetworkManager.ServerTime.Time + _lifetime : 0;
        }
        Sprite[] sprites = new Sprite[_runes.Count];
        EnchantData[] enchants = new EnchantData[_runes.Count];
        for (int i = 0; i < _runes.Count; i++)
        {
            NetworkRuneState rune = _runes[i];
            if (rune.SpriteIndex < 0 || rune.SpriteIndex >= _runeSprites.Length)
                return;
            sprites[i] = rune.EnchantId == 0 ? _charredRuneSprites[rune.SpriteIndex] : _runeSprites[rune.SpriteIndex];
            enchants[i] = rune.EnchantId == 0 ? null : _enchantDatabase.GetEnchant(rune.EnchantId);
            if (rune.EnchantId != 0 && enchants[i] == null)
                return;
        }
        _enchanter.Initialize(sprites, enchants, EventLevel == 2);
        _runes.OnListChanged += ApplyRuneStates;
        ApplyRuneStates(default);
        if (IsServer)
        {
            _enchanter.RuneBroken += OnRuneBroken;
            if (_announceAppearance)
                PlayerHUD.NotifyPlayers(NetworkManager, "Появился чарователь", new Color32(194, 163, 224, 255));
        }
    }

    private void Update()
    {
        if (IsSpawned && IsServer && _expiresAt > 0 && NetworkManager.ServerTime.Time >= _expiresAt)
            NetworkObject.Despawn();
    }

    private void OnRuneBroken(int index)
    {
        if (_runes[index].IsBroken)
            return;
        BreakRuneRpc(index);
        for (int i = 0; i < _runes.Count; i++)
        {
            if (EventLevel != 2 && i != index)
                continue;
            NetworkRuneState rune = _runes[i];
            rune.IsBroken = true;
            _runes[i] = rune;
        }
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void BreakRuneRpc(int index) => _enchanter.Runes[index].GetComponent<RuneVisuals>().PlayBreakEffect();

    private void ApplyRuneStates(NetworkListEvent<NetworkRuneState> change)
    {
        for (int i = 0; i < _runes.Count; i++)
            _enchanter.Runes[i].SetBroken(_runes[i].IsBroken);
    }

    public override void OnNetworkDespawn()
    {
        _runes.OnListChanged -= ApplyRuneStates;
        _enchanter.RuneBroken -= OnRuneBroken;
    }
}
